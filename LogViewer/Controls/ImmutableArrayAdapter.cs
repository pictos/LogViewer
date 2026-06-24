using Nalu;
using System.Collections.Immutable;

namespace LogViewer.Controls;

sealed class ImmutableArrayAdapter : IVirtualScrollAdapter
{
	private readonly ImmutableArray<LogInfo> array;

	public ImmutableArrayAdapter(ImmutableArray<LogInfo> array)
	{
		this.array = array;
	}

	public object? GetItem(int sectionIndex, int itemIndex)
	{
		var a = array;
		if ((uint)itemIndex < (uint)a.Length)
		{
			a[itemIndex].Index = itemIndex;
			return a[itemIndex];
		}
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
