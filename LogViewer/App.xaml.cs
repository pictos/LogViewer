using XmlnsPrefixAttribute = Microsoft.Maui.Controls.XmlnsPrefixAttribute;

[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.ViewModels")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Models")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Controls")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Controls.Behaviors")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Pages")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Popups")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "http://schemas.microsoft.com/dotnet/2022/maui/toolkit")]

[assembly: XmlnsPrefix("http://schemas.microsoft.com/dotnet/2022/maui/toolkit", "mct")]

namespace LogViewer;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		_ = MainThreadSwitcher;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}