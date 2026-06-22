using PJ.Core;
using XmlnsPrefixAttribute = Microsoft.Maui.Controls.XmlnsPrefixAttribute;

[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.ViewModels")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Models")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "LogViewer.Pages")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "http://schemas.microsoft.com/dotnet/2022/maui/toolkit")]

[assembly: XmlnsPrefix("http://schemas.microsoft.com/dotnet/2022/maui/toolkit", "mct")]

namespace LogViewer;

public partial class App : Application
{
	internal static PJ.Core.UIThreadManager UIThreadManager { get; private set; } = default!;

	public App()
	{
		InitializeComponent();
		UIThreadManager = new PJ.Core.UIThreadManager(SynchronizationContext.Current!);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}