
using Microsoft.Win32.SafeHandles;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


namespace LogViewer.Parsers;

// based on https://github.com/buybackoff/1brc
public unsafe sealed partial class LoggerReader : IDisposable
{
	readonly FileStream stream;
	readonly MemoryMappedFile mmf;
	readonly MemoryMappedViewAccessor va;
	readonly SafeMemoryMappedViewHandle vaHandle;
	readonly byte* pointer;
	readonly long fileLength;

	readonly int initialChunkCount;
	readonly int finalListSize;
	const int MaxChunkSize = int.MaxValue - 100_000;

	public string FilePath { get; }

	static double[] powersOf10 = new double[64];
	static GCHandle powersHandle;
	readonly double* powersPtr = Init10Powers();
	ImmutableArray<string>? lines;

	public LoggerReader(string filePath, int? chunckCount = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(filePath);
		initialChunkCount = Math.Max(1, chunckCount ?? Environment.ProcessorCount);
		FilePath = filePath;

		stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.SequentialScan);
		fileLength = stream.Length;
		mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);

		byte* ptr = (byte*)0;
		va = mmf.CreateViewAccessor(0, fileLength, MemoryMappedFileAccess.Read);
		vaHandle = va.SafeMemoryMappedViewHandle;
		vaHandle.AcquirePointer(ref ptr);
		finalListSize = (int)(stream.Length / 186);
		pointer = ptr;
	}

	public List<(long start, int end)> SplitIntoMemoryChunks()
	{
		var chunkCount = initialChunkCount;
		var chunkSize = fileLength / chunkCount;

		while (chunkSize > MaxChunkSize)
		{
			chunkCount *= 2;
			chunkSize = fileLength / chunkCount;
		}

		var chunks = new List<(long, int)>(chunkCount);
		var pos = 0L;

		for (var i = 0; i < chunkCount; i++)
		{
			if (pos + chunkSize >= fileLength)
			{
				chunks.Add((pos, (int)(fileLength - pos)));
				break;
			}

			var newPos = pos + chunkSize;
			var sp = new ReadOnlySpan<byte>(pointer + newPos, (int)chunkSize);
			var idx = IndexOfNewLineChar(sp, out var stride);
			newPos += idx + stride;
			var len = newPos - pos;
			chunks.Add((pos, (int)(len)));
			pos = newPos;
		}

		return chunks;
	}

	public List<string> ProcessChunkFilter(long start, int length, ReadOnlySpan<char> query)
	{
		Span<char> lowerquery = stackalloc char[query.Length];
		query.ToLowerInvariant(lowerquery);
		var remaining = new Utf8Span(pointer + start, length);

		int maxByteCount = Encoding.UTF8.GetMaxByteCount(lowerquery.Length);

		Span<byte> utf8TargetBuffer = maxByteCount <= 1024
			? stackalloc byte[maxByteCount]
			: new byte[maxByteCount];

		int actualBytesWritten = Encoding.UTF8.GetBytes(lowerquery, utf8TargetBuffer);
		ReadOnlySpan<byte> utf8Target = utf8TargetBuffer[..actualBytesWritten];

		var result = new List<string>(128);

		while (remaining.Length > 0)
		{
			var idx = remaining.Span.IndexOf(newLine);
			var lineLength = idx >= 0 ? idx : remaining.Length;
			var value = remaining.SliceUnsafe(0, lineLength);
			var span = value.Span;

			if (IndexOfIgnoreCaseAscii(span, utf8Target) >= 0)
			{
				var log = Encoding.UTF8.GetString(span);
				result.Add(log);
			}

			int advance = idx >= 0 ? idx + 1 : remaining.Length;
			remaining = remaining.SliceUnsafe(advance);
		}

		return result;
	}

	public ImmutableArray<string> Filter(string query)
	{
		return SplitIntoMemoryChunks()
			.AsParallel()
			.AsOrdered()
			.Select(tuple => ProcessChunkFilter(tuple.start, tuple.end, query))
			.Aggregate(
				() => new List<string>(256),
				(result, chunkMatches) => { result.AddRange(chunkMatches); return result; },
				(finalResult, localResult) => { finalResult.AddRange(localResult); return finalResult; },
				finalResult => finalResult.ToImmutableArray());
	}


	[MemberNotNull(nameof(lines))]
	public ImmutableArray<string> Process()
	{
		lines ??= SplitIntoMemoryChunks()
			.AsParallel()
			.AsOrdered()
			.Select(tuple => ProcessChunk(tuple.start, tuple.end))
			.Aggregate(
				() => new List<string>(finalListSize),
				(result, chunk) => { result.AddRange(chunk); return result; },
				(finalResult, localResult) => { finalResult.AddRange(localResult); return finalResult; },
				finalResult => finalResult.ToImmutableArray()
			);

		return lines.Value;
	}

	public List<string> ProcessChunk(long start, int length)
	{
		var remaining = new Utf8Span(pointer + start, length);

		var result = new List<string>(512);

		while (remaining.Length > 0)
		{
			var idx = remaining.Span.IndexOf(newLine);

			int lineLength = idx >= 0 ? idx : remaining.Length;

			var value = remaining.SliceUnsafe(0, lineLength);
			result.Add(Encoding.UTF8.GetString(value.Span));

			int advance = idx >= 0 ? idx + 1 : remaining.Length;
			remaining = remaining.SliceUnsafe(advance);
		}

		return result;
	}

	const byte newLine = (byte)'\n';
	const byte carriageReturn = (byte)'\r';
	const byte semiCollon = (byte)';';

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static byte ToLowerAscii(byte b) => (uint)(b - (byte)'A') <= (byte)'Z' - (byte)'A' ? (byte)(b | 0x20) : b;

	internal static int IndexOfIgnoreCaseAscii(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> lowerNeedle)
	{
		if (lowerNeedle.IsEmpty)
			return 0;

		byte firstLower = lowerNeedle[0];
		byte firstUpper = (uint)(firstLower - (byte)'a') <= (byte)'z' - (byte)'a'
			? (byte)(firstLower & ~0x20)
			: firstLower;

		var offset = 0;
		while (true)
		{
			var slice = haystack.Slice(offset);
			var i = firstLower == firstUpper
				? slice.IndexOf(firstLower)
				: slice.IndexOfAny(firstLower, firstUpper);

			if (i < 0)
				return -1;

			var pos = offset + i;
			if (pos + lowerNeedle.Length > haystack.Length)
				return -1;

			if (MatchesIgnoreCaseAscii(haystack.Slice(pos, lowerNeedle.Length), lowerNeedle))
				return pos;

			offset = pos + 1;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool MatchesIgnoreCaseAscii(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> lowerNeedle)
	{
		var size = lowerNeedle.Length;
		for (var j = 0; j < size; j++)
		{
			if (ToLowerAscii(candidate[j]) != lowerNeedle[j])
				return false;
		}

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfNewLineChar(ReadOnlySpan<byte> span, out int stride)
	{
		stride = default;
		var idx = span.IndexOfAny(newLine, carriageReturn);
		if ((uint)idx < (uint)span.Length)
		{
			stride = 1;
			if (span[idx] == carriageReturn)
			{
				var nextCharIdx = idx + 1;
				if ((uint)nextCharIdx < (uint)span.Length && span[nextCharIdx] == newLine)
				{
					stride = 2;
				}
			}
		}

		return idx;
	}

	public void Dispose()
	{
		vaHandle.Dispose();
		va.Dispose();
		mmf.Dispose();
		stream.Dispose();
		GC.SuppressFinalize(this);
	}

	static double* Init10Powers()
	{
		for (var i = 0; i < 64; i++)
		{
			powersOf10[i] = 1 / Math.Pow(10, i);
		}

		powersHandle = GCHandle.Alloc(powersOf10, GCHandleType.Pinned);
		return (double*)powersHandle.AddrOfPinnedObject();
	}
}
