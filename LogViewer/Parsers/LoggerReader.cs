using Microsoft.Win32.SafeHandles;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace LogViewer.Parsers;

// based on https://github.com/buybackoff/1brc
[SkipLocalsInit]
public unsafe sealed partial class LoggerReader : IDisposable
{
	const byte newLine = (byte)'\n';
	const byte carriageReturn = (byte)'\r';
	const byte semiCollon = (byte)';';

	readonly MemoryMappedFile mmf;
	readonly MemoryMappedViewAccessor va;
	readonly SafeMemoryMappedViewHandle vaHandle;
	readonly byte* pointer;
	readonly long fileLength;

	const byte LF = (byte)'\n';
	const byte CR = (byte)'\r';

	readonly int initialChunkCount;
	const int MaxChunkSize = int.MaxValue - 100_000;

	public string FilePath { get; }

	ImmutableArray<LogInfo>? lines;

	public LoggerReader(string filePath, int? chunkCount = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(filePath);
		initialChunkCount = Math.Max(1, chunkCount ?? Environment.ProcessorCount);
		FilePath = filePath;

		fileLength = new FileInfo(filePath).Length;
		mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);

		byte* ptr = (byte*)0;
		va = mmf.CreateViewAccessor(0, fileLength, MemoryMappedFileAccess.Read);
		vaHandle = va.SafeMemoryMappedViewHandle;
		vaHandle.AcquirePointer(ref ptr);
		pointer = ptr;
	}

	/// <summary>
	/// Splits the file into roughly equal chunks, each ending on a line boundary, so chunks can be
	/// parsed/searched in parallel without splitting a line across two chunks.
	/// </summary>
	public List<(long start, int length)> SplitIntoMemoryChunks()
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
			chunks.Add((pos, (int)len));
			pos = newPos;
		}

		return chunks;
	}

	/// <summary>
	/// Parses every line into a <see cref="LogInfo"/> over the mapped bytes. The result is computed
	/// once and cached for the lifetime of the parser. No line text is decoded here.
	/// </summary>
	public ImmutableArray<LogInfo> Process()
	{
		if (lines is { } cached)
			return cached;

		var immutable = ParseUncached();
		lines = immutable;
		return immutable;
	}

	/// <summary>
	/// Performs the full parse without reading or writing the cache. The memory map is set up once
	/// in the constructor, so this measures the parse work in isolation.
	/// </summary>
	public ImmutableArray<LogInfo> ParseUncached()
	{
		if (fileLength is 0)
			return [];

		var chunks = SplitIntoMemoryChunks();
		var n = chunks.Count;


		var offsets = new int[n];
		var total = 0;
		for (var i = 0; i < n; i++)
		{
			var (start, length) = chunks[i];
			offsets[i] = total;
			total += CountLines(new ReadOnlySpan<byte>(pointer + start, length));
		}

		var result = new LogInfo[total];
		Parallel.For(0, n, i =>
		{
			var (start, length) = chunks[i];
			FillChunk(start, length, result, offsets[i]);
		});

		return ImmutableCollectionsMarshal.AsImmutableArray(result);
	}

	[SkipLocalsInit]
	void FillChunk(long start, int length, LogInfo[] dest, int destOffset)
	{
		var chunkMemory = new UnmanagedMemoryManager<byte>(pointer + start, length).Memory;
		var span = chunkMemory.Span;

		var consumed = 0;
		var w = destOffset;

		while (consumed < length)
		{
			var rest = span.Slice(consumed);
			var nl = rest.IndexOf(LF);

			int contentLength, advance;
			if (nl < 0)
			{
				contentLength = rest.Length;
				advance = rest.Length;
			}
			else
			{
				contentLength = nl;
				advance = nl + 1;
			}

			if (contentLength > 0 && rest[contentLength - 1] == CR)
				contentLength--;

			dest[w++] = new LogInfo(chunkMemory.Slice(consumed, contentLength));
			consumed += advance;
		}
	}

	/// <summary>
	/// Returns the lines that match the boolean query, evaluated case-insensitively over ASCII.
	/// The query supports <c>&amp;</c> (AND), <c>|</c> (OR), <c>!</c> (NOT), parentheses, and
	/// quoted phrases. Simple terms (no operators) behave exactly as before.
	/// The search runs on raw UTF-8 bytes; a line's <see cref="string"/> is only decoded on demand.
	/// </summary>
	/// <example>
	/// <code>
	/// reader.Filter("Maui &amp; close")     // lines containing both "Maui" and "close"
	/// reader.Filter("!close")              // lines that do NOT contain "close"
	/// reader.Filter("error | warning")    // lines containing "error" or "warning"
	/// reader.Filter("(error | warn) &amp; !debug")
	/// </code>
	/// </example>
	public ImmutableArray<LogInfo> Filter(string query)
	{
		ArgumentException.ThrowIfNullOrEmpty(query);
		var node = QueryParser.Parse(query);
		return Filter(node);
	}

	/// <summary>
	/// Returns the lines that satisfy <paramref name="query"/>, evaluated in parallel over
	/// memory-mapped chunks. No line text is decoded unless <see cref="LogInfo.Text"/> is read.
	/// </summary>
	internal ImmutableArray<LogInfo> Filter(QueryNode query)
	{
		ArgumentNullException.ThrowIfNull(query);

		if (fileLength == 0)
			return ImmutableArray<LogInfo>.Empty;

		return SplitIntoMemoryChunks()
			.AsParallel()
			.AsOrdered()
			.Select(tuple => FilterChunk(tuple.start, tuple.length, query))
			.Aggregate(
				() => new List<LogInfo>(64),
				(acc, chunk) => { acc.AddRange(chunk); return acc; },
				(a, b) => { a.AddRange(b); return a; },
				acc => acc.ToImmutableArray());
	}

	[SkipLocalsInit]
	List<LogInfo> FilterChunk(long start, int length, QueryNode query)
	{
		var chunkMemory = new UnmanagedMemoryManager<byte>(pointer + start, length).Memory;
		var span = chunkMemory.Span;

		var result = new List<LogInfo>(16);
		var consumed = 0;

		while (consumed < length)
		{
			var rest = span.Slice(consumed);
			var nl = rest.IndexOf(LF);

			int contentLength, advance;
			if (nl < 0)
			{
				contentLength = rest.Length;
				advance = rest.Length;
			}
			else
			{
				contentLength = nl;
				advance = nl + 1;
			}

			if (contentLength > 0 && rest[contentLength - 1] == CR)
				contentLength--;

			if (query.Matches(rest.Slice(0, contentLength)))
				result.Add(new LogInfo(chunkMemory.Slice(consumed, contentLength)));

			consumed += advance;
		}

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static byte ToLowerAscii(byte b) =>
	(uint)(b - (byte)'A') <= (byte)'Z' - (byte)'A' ? (byte)(b | 0x20) : b;

	/// <summary>
	/// Case-insensitive (ASCII) substring search. <paramref name="lowerNeedle"/> must already be
	/// lower-cased. Anchors on the first byte (either case) using a vectorized
	/// <see cref="MemoryExtensions.IndexOfAny{T}(ReadOnlySpan{T}, T, T)"/>, then verifies the rest.
	/// </summary>
	internal static int IndexOfIgnoreCaseAscii(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> lowerNeedle)
	{
		if (lowerNeedle.IsEmpty)
			return 0;

		var firstLower = lowerNeedle[0];
		var firstUpper = (uint)(firstLower - (byte)'a') <= (byte)'z' - (byte)'a'
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
		for (var j = 0; j < lowerNeedle.Length; j++)
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int CountLines(ReadOnlySpan<byte> span)
	{
		if (span.Length == 0)
			return 0;

		var count = span.Count(LF);

		if (span[^1] != LF)
			count++;

		return count;
	}

	public void Dispose()
	{
		vaHandle.Dispose();
		va.Dispose();
		mmf.Dispose();
		GC.SuppressFinalize(this);
	}
}
