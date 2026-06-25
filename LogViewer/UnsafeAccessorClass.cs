#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif
using Nalu;
using System.Runtime.CompilerServices;

namespace LogViewer;

class UnsafeAccessorClass
{
#if WINDOWS
	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_scrollViewer")]
	public static extern ref ScrollViewer GetScrollViewer(VirtualScrollHandler handler);
#endif

	UnsafeAccessorClass() { }
}