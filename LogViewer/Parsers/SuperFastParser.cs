
using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace LogViewer.Parsers;

public unsafe class SuperFastParser : IDisposable
{
	readonly FileStream stream;
	readonly MemoryMappedFile mmf;
	readonly MemoryMappedViewAccessor va;
	readonly SafeMemoryMappedViewHandle vaHandle;
	readonly byte* pointer;
	readonly long fileLength;

	readonly int initialChunkCount;
	const int MaxChunkSize = int.MaxValue - 100_000;

	public string FilePath { get; }

	static double[] powersOf10 = new double[64];
	static GCHandle powersHandle;
	readonly double* powersPtr = Init10Powers();
	ImmutableArray<LogLine2>? lines;

	public SuperFastParser(string filePath, int? chunckCount = null)
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

	public List<LogLine2> ProcessChunk(long start, int length)
	{
		var remaining = new Utf8Span(pointer + start, length);

		var result = new List<LogLine2>(512);

		while (remaining.Length > 0)
		{
			var idx = remaining.Span.IndexOf(newLine);
			var value = remaining.SliceUnsafe(0, idx);
			var log = new LogLine2(Encoding.UTF8.GetString(value.Span));
			result.Add(log);
			remaining = remaining.SliceUnsafe(idx + 1);
		}

		return result;
	}

	public List<LogLine2> ProcessChunkFilter(long start, int length, ReadOnlySpan<char> query)
	{
		var remaining = new Utf8Span(pointer + start, length);
		var result = new List<LogLine2>(1024);

		Span<char> lineCharBuffer = stackalloc char[1024];
		char[]? lineArray = null;
		while (remaining.Length > 0)
		{
			var idx = remaining.Span.IndexOf(newLine);
			var lineLength = idx >= 0 ? idx : remaining.Length;
			var value = remaining.SliceUnsafe(0, lineLength);

			Span<char> decodedLine = lineCharBuffer;
			int maxCharsNeeded = Encoding.UTF8.GetMaxCharCount(value.Length);

			if (maxCharsNeeded > lineCharBuffer.Length)
			{
				if (lineArray is not null)
				{
					ArrayPool<char>.Shared.Return(lineArray);
				}
				lineArray = ArrayPool<char>.Shared.Rent(maxCharsNeeded);
				decodedLine = lineArray;
			}

			int charsWritten = Encoding.UTF8.GetChars(value.Span, decodedLine);
			ReadOnlySpan<char> lineAsChars = decodedLine[..charsWritten];

			if (lineAsChars.Contains(query, StringComparison.OrdinalIgnoreCase))
			{
				var text = new string(lineAsChars);
				var log = new LogLine2(text);
				result.Add(log);
			}

			if (idx < 0) break;
			remaining = remaining.SliceUnsafe(idx + 1);
		}

		if (lineArray is not null)
		{
			ArrayPool<char>.Shared.Return(lineArray);
		}

		return result;
	}

	public ImmutableArray<LogLine2> Filter(string query)
	{
		return SplitIntoMemoryChunks()
		.AsParallel()
		.AsOrdered()
		.Select(tuple => ProcessChunkFilter(tuple.start, tuple.end, query))
		.Aggregate((result, chunk) =>
		{
			result.AddRange(chunk);
			return result;
		})
		.ToImmutableArray();
	}

	public ImmutableArray<LogLine2> Process()
	{
		lines ??= SplitIntoMemoryChunks()
		.AsParallel()
		.AsOrdered()
		.Select(tuple => ProcessChunk(tuple.start, tuple.end))
		.ToList()
		.Aggregate((result, chunk) =>
		{
			result.AddRange(chunk);
			return result;
		}).ToImmutableArray();

		return lines.Value;
	}

	const byte newLine = (byte)'\n';
	const byte carriageReturn = (byte)'\r';
	const byte semiCollon = (byte)';';

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


