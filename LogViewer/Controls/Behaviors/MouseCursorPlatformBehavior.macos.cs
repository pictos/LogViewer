using AppKit;
using LogViewer.Models;
using UIKit;

namespace LogViewer.Controls.Behaviors;

partial class MouseCursorPlatformBehavior : PlatformBehavior<VisualElement, UIView>
{
	 UIHoverGestureRecognizer? gestureRecognizer;
	protected override void OnAttachedTo(VisualElement bindable, UIView platformView)
	{
		gestureRecognizer ??= new UIHoverGestureRecognizer((r) =>
		{
			switch (r.State)
			{
				case UIGestureRecognizerState.Began:
					HoverCursor.ToPlatform().Set();
					break;
				case UIGestureRecognizerState.Changed:
					break;
				case UIGestureRecognizerState.Cancelled:
				case UIGestureRecognizerState.Ended:
					NSCursor.ArrowCursor.Set();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		});

		platformView.AddGestureRecognizer(gestureRecognizer);
	}

	protected override void OnDetachedFrom(VisualElement bindable, UIView platformView)
	{
		Assert(gestureRecognizer is not null);
		platformView.RemoveGestureRecognizer(gestureRecognizer);
	}
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

