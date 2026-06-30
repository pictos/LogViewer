namespace LogViewer.Pages;

sealed partial class PopupPage : ContentPage
{
	public PopupPage(View view, Color? background = null)
	{
		ArgumentNullException.ThrowIfNull(view);
		Background = background ?? Colors.Transparent;

		Shell.SetPresentationMode(this, PresentationMode.NotAnimated);

		view.HorizontalOptions = view.VerticalOptions = LayoutOptions.Center;

		Content = view;
	}
}
