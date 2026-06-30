using LogViewer.Models;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace LogViewer.Controls.Behaviors;

partial class MouseCursorPlatformBehavior : PlatformBehavior<VisualElement, UIElement>
{
	InputCursor wCursor = default!;
	static readonly InputCursor arrowCursor = MouseCursor.Default.ToPlatform();

	protected override void OnAttachedTo(VisualElement bindable, UIElement platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		wCursor = HoverCursor.ToPlatform();

		platformView.PointerEntered += OnPointerEntered;
		platformView.PointerExited += OnPointerExited;
	}

	protected override void OnDetachedFrom(VisualElement bindable, UIElement platformView)
	{
		base.OnDetachedFrom(bindable, platformView);
		platformView.PointerExited -= OnPointerExited;
		platformView.PointerEntered -= OnPointerEntered;
	}

	void OnPointerEntered(object sender, PointerRoutedEventArgs e)
	{
		var uiElement = (UIElement)sender;
		
		uiElement.ChangeCursor(wCursor);
	}

	static void OnPointerExited(object sender, PointerRoutedEventArgs e)
	{
		var uiElement = (UIElement)sender;
		uiElement.ChangeCursor(arrowCursor);
	}
}
