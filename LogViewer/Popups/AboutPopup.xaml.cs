using LogViewer.Services;

namespace LogViewer.Popups;

public partial class AboutPopup
{
	public AboutPopup()
	{
		InitializeComponent();
	}

	async void OnContactTapped(object? sender, TappedEventArgs e)
	{
		await Launcher.Default.OpenAsync(new Uri("mailto:pedrojesus@softwarepj.onmicrosoft.com"));
	}

	async void OnCloseClicked(object? sender, EventArgs e)
	{
		await NavigationService.RemovePopupAsync();
	}
}