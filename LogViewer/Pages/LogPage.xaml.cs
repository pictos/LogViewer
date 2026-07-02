using LogViewer.Services;

namespace LogViewer;

public partial class LogPage : ContentPage
{
	public LogPage()
	{
		InitializeComponent();
	}

	async void DropGestureRecognizer_Drop(object sender, DropEventArgs e)
	{

		if (e.PlatformArgs is not PlatformDropEventArgs dropArgs || tabsLayout.Children.Count > 0)
		{
			return;
		}
		e.Handled = true;

#if WINDOWS
		await DropFileService.HandleDragNewTab(dropArgs);
#endif
	}
}