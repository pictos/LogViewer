using Nalu;
using System.Collections.Immutable;

namespace LogViewer.Controls;

sealed class ImmutableArrayAdapter<T> : IVirtualScrollAdapter
{
	private readonly ImmutableArray<T> array;

	public ImmutableArrayAdapter(ImmutableArray<T> array)
	{
		this.array = array;
	}

	public object? GetItem(int sectionIndex, int itemIndex)
	{
		if ((uint)itemIndex < (uint)array.Length)
			return array[itemIndex];
		return null;
	}

	public int GetItemCount(int sectionIndex) => array.Length;

	public object? GetSection(int sectionIndex) => null;

	public int GetSectionCount() => array.Length > 0 ? 1 : 0;

	public IDisposable Subscribe(Action<VirtualScrollChangeSet> changeCallback) => D.Empty;

	sealed partial class D : IDisposable
	{
		public static D Empty { get; } = new();

		D()
		{
			
		}

		public void Dispose()
		{

		}
	}
}
