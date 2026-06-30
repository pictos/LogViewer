using LogViewer.Pages;

namespace LogViewer;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(PopupPage), typeof(PopupPage));
	}
}
