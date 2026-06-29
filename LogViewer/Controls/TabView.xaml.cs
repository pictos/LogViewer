namespace LogViewer.Controls;

public partial class TabView
{
	static readonly Color SelectedBackground = Color.FromArgb("#3D5A80");
	static readonly Color UnselectedBackground = Colors.Black;

	public required LogView LogView { get; init; }

	public TabView()
	{
		InitializeComponent();
	}

	public void SetSelected(bool isSelected) =>
		Background = isSelected ? SelectedBackground : UnselectedBackground;

	static void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		var tabView = (TabView)sender;
		var logPage = (LogPage)Shell.Current.CurrentPage;

		if (tabView.LogView.Group is SideBySideContent group)
		{
			logPage.ShowGroup(group);
		}
	}
}