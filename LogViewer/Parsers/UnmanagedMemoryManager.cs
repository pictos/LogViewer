using System.Buffers;
using System.Runtime.CompilerServices;


namespace LogViewer.Parsers;

/// <summary>
/// Exposes a region of unmanaged memory (e.g. a memory-mapped file) as <see cref="Memory{T}"/>
/// without copying. One instance can back many slices; slicing the produced <see cref="Memory{T}"/>
/// does not allocate.
/// </summary>
public sealed unsafe partial class UnmanagedMemoryManager<T> : MemoryManager<T> where T : unmanaged
{
	readonly T* pointer;
	readonly int length;

	public UnmanagedMemoryManager(T* pointer, int length)
	{
		this.pointer = pointer;
		this.length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override Span<T> GetSpan() => new(pointer, length);

	public override MemoryHandle Pin(int elementIndex = 0) => new(pointer + elementIndex);

	public override void Unpin() { }

	protected override void Dispose(bool disposing) { }
}