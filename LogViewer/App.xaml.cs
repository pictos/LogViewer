[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.ViewModels")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Models")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Pages")]

namespace LogViewer;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}