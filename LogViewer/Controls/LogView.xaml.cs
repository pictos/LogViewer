namespace LogViewer.Controls;

public partial class LogView
{
	public bool IsMain { get; set; }

	public SideBySideContent? Group { get; set; }

	public LogView()
	{
		InitializeComponent();
	}
}