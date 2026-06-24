using DeviceRunners.VisualRunners;
using LogViewer.Helpers;
using Microsoft.Extensions.Logging;

namespace LogViewer.DeviceTests;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		_ = MainThreadManager.MainThreadSwitcher;
		var builder = MauiApp.CreateBuilder();
		builder
			.UseVisualTestRunner(conf => conf
				.AddNUnit()
				.AddConsoleResultChannel()
				.AddTestAssembly(typeof(MauiProgram).Assembly))
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
