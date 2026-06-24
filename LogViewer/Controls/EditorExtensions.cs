using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#endif

namespace LogViewer.Controls;

sealed class EditorExtensions
{
	public static void InitHandler()
	{
		EditorHandler.PlatformViewFactory = (handler) =>
		{
#if WINDOWS
			var platformView = new TextBox
			{
				AcceptsReturn = true,
				TextWrapping = TextWrapping.Wrap,
				BorderThickness = new Microsoft.UI.Xaml.Thickness(0),
				BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
				FocusVisualPrimaryThickness = new Microsoft.UI.Xaml.Thickness(0),
				FocusVisualSecondaryThickness = new Microsoft.UI.Xaml.Thickness(0)
			};
			platformView.Resources["TextControlBorderThemeThickness"] = new Microsoft.UI.Xaml.Thickness(0);
			platformView.Resources["TextControlBorderThemeThicknessFocused"] = new Microsoft.UI.Xaml.Thickness(0);
			platformView.Resources["TextControlBorderThemeThicknessPointerOver"] = new Microsoft.UI.Xaml.Thickness(0);
			return platformView;
#else
			var platformEditor = new MauiTextView();

			//platformEditor.AddMauiDoneAccessoryView(handler);
			platformEditor.BorderStyle = UIKit.UITextViewBorderStyle.None;
			return platformEditor;
#endif
		};
	}
}
