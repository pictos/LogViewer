namespace LogViewer.Controls;

public partial class TabView
{
	public required LogView LogView { get; init; }

	public TabView()
	{
		InitializeComponent();
	}

	static void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		var tabView = (TabView)sender;
		var logPage = (LogPage)Shell.Current.CurrentPage;

		logPage.HideAllTabs();

		tabView.LogView.IsVisible = true;
	}
}