using AppKit;
using LogViewer.Models;
using UIKit;

namespace LogViewer.Controls.Behaviors;

partial class MouseCursorPlatformBehavior : PlatformBehavior<VisualElement, UIView>
{

}

static class MouseCursorExtensions
{
	public static NSCursor ToPlatform(this MouseCursor cursor) => cursor switch
	{
		MouseCursor.Hand => NSCursor.PointingHandCursor,
		MouseCursor.SizeWestEast => NSCursor.ResizeLeftRightCursor,
		_ => NSCursor.ArrowCursor
	};
}

