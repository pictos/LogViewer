using LogViewer.Models;
using Microsoft.UI.Xaml;
using System.Reflection;
using Microsoft.UI.Input;
using static System.Reflection.BindingFlags;
using Windows.UI.Core;

namespace LogViewer.Helpers;

public static class VisualElementExtensions
{
	static PropertyInfo? propInfo;

	public static void ChangeCursor(this UIElement element, InputCursor cursor)
	{
		propInfo ??= typeof(UIElement).GetProperty("ProtectedCursor", Instance | NonPublic | SetProperty);
		Assert(propInfo is not null);
		propInfo.SetValue(element, cursor);
	}

	public static InputCursor ToPlatform(this MouseCursor cursor)
	{
		var coreCursor = cursor switch
		{
			MouseCursor.Default => CoreCursorType.Arrow,
			MouseCursor.SizeWestEast => CoreCursorType.SizeWestEast,
			_ => CoreCursorType.Arrow
		};

		var inputCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(coreCursor, 1));

		return inputCursor;
	}
}

//class B : StackPanel
//{
//	public B()
//	{
//		ProtectedCursor
//	}
//}