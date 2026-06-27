#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif
using Nalu;
using System.Runtime.CompilerServices;
using ColumnDefinition = Microsoft.Maui.Controls.ColumnDefinition;
using RowDefinition = Microsoft.Maui.Controls.RowDefinition;

namespace LogViewer;

class UnsafeAccessorClass
{
#if WINDOWS
	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_scrollViewer")]
	public static extern ref ScrollViewer GetScrollViewer(VirtualScrollHandler handler);
#endif

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ActualWidth")]
	public static extern double GetUnsafeActualWidth(ColumnDefinition definition);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ActualHeight")]
	public static extern double GetUnsafeActualHeight(RowDefinition definition);

	UnsafeAccessorClass() { }
}