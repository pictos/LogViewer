using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LogViewer.Parsers;

/// <summary>
/// Cached filtering for <see cref="LoggerReader"/>. Because the mapped file is immutable for the
/// reader's lifetime, a query's result is immutable too, so it can be memoized. Two wins:
/// <list type="number">
///   <item><description><b>Exact memoization</b> — a repeated query (by canonical
///   <see cref="QueryNode.Key"/>) returns the cached result in O(1) without rescanning.</description></item>
///   <item><description><b>Incremental narrowing</b> — <c>A &amp; B</c> is always a subset of the
///   lines matching <c>A</c>, so <c>B</c> is evaluated only over the (cached) result of <c>A</c>.
///   <c>A</c> is cached recursively, so its scan is reused by every later <c>A &amp; …</c>
///   query.</description></item>
/// </list>
/// All cached results reference the same <see cref="LogInfo"/> instances produced by
/// <see cref="LoggerReader.Process"/>, in file order, so caching adds only one reference per matched
/// line and preserves ordering. <see cref="LogInfo.Index"/> is assigned at render time by the view
/// adapter, so sharing instances across the full and filtered views is safe.
/// </summary>
public unsafe sealed partial class LoggerReader
{
	readonly ConcurrentDictionary<string, ImmutableArray<LogInfo>> filterCache = new();
	const int FilterCacheMaxEntries = 256;

	/// <summary>Clears the memoized filter results (e.g. before reusing the reader for a new view).</summary>
	public void ClearFilterCache() => filterCache.Clear();

	ImmutableArray<LogInfo> EvaluateCached(QueryNode node)
	{
		if (fileLength == 0)
			return ImmutableArray<LogInfo>.Empty;

		if (filterCache.TryGetValue(node.Key, out var hit))
			return hit;

		// A & B ⊆ A: evaluate B only over the (recursively cached) lines matching A.
		var result = node is AndNode and
			? EvaluateOver(and.Right, EvaluateCached(and.Left))
			: EvaluateOver(node, Process());

		if (filterCache.Count < FilterCacheMaxEntries)
			filterCache.TryAdd(node.Key, result);

		return result;
	}

	/// <summary>
	/// Evaluates <paramref name="node"/> against an already-parsed candidate set, testing each
	/// line's bytes directly (no rescanning for newlines). Order is preserved.
	/// </summary>
	public ImmutableArray<LogInfo> EvaluateOver(QueryNode node, ImmutableArray<LogInfo> candidates)
	{
		ArgumentNullException.ThrowIfNull(node);

		if (candidates.IsDefaultOrEmpty)
			return ImmutableArray<LogInfo>.Empty;

		var src = candidates;
		var n = src.Length;

		const int SequentialThreshold = 2048;
		if (n <= SequentialThreshold)
		{
			var seq = new List<LogInfo>(Math.Max(16, n / 8));
			for (var i = 0; i < n; i++)
			{
				var line = src[i];
				if (node.Matches(line.LineBytes.Span))
					seq.Add(line);
			}

			return seq.Count == 0
				? ImmutableArray<LogInfo>.Empty
				: ImmutableCollectionsMarshal.AsImmutableArray(seq.ToArray());
		}

		var partitions = Environment.ProcessorCount;
		var perPart = new List<LogInfo>[partitions];
		var chunk = (n + partitions - 1) / partitions;

		Parallel.For(0, partitions, p =>
		{
			var startIdx = p * chunk;
			if (startIdx >= n)
			{
				perPart[p] = [];
				return;
			}

			var endIdx = Math.Min(startIdx + chunk, n);
			var local = new List<LogInfo>(Math.Max(16, (endIdx - startIdx) / 8));
			for (var i = startIdx; i < endIdx; i++)
			{
				var line = src[i];
				if (node.Matches(line.LineBytes.Span))
					local.Add(line);
			}

			perPart[p] = local;
		});

		var total = 0;
		for (var p = 0; p < partitions; p++)
			total += perPart[p].Count;

		if (total == 0)
			return ImmutableArray<LogInfo>.Empty;

		var result = new LogInfo[total];
		var w = 0;
		for (var p = 0; p < partitions; p++)
		{
			var list = perPart[p];
			list.CopyTo(result, w);
			w += list.Count;
		}

		return ImmutableCollectionsMarshal.AsImmutableArray(result);
	}
}
