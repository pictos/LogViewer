#if WINDOWS
using Windows.ApplicationModel;
#endif

namespace LogViewer.DeviceTests;

public static class General
{

#if WINDOWS
	static readonly Lazy<bool> isPackagedAppLazy = new(() =>
	{
		try
		{
			if (Package.Current is not null)
				return true;
		}
		catch
		{
			// no-op
		}

		return false;
	});

	public static string BasePath => isPackagedAppLazy.Value
		? Package.Current.InstalledLocation.Path
		: AppContext.BaseDirectory;
	public static bool IsPackagedApp => isPackagedAppLazy.Value;

	public static string FullAppPackageFilePath => BasePath;
#elif MACCATALYST || IOS
	// On Apple platforms, MauiAsset (Resources/Raw) files are bundled into the
	// app's Contents/Resources directory, which is NSBundle.MainBundle.ResourcePath.
	// AppContext.BaseDirectory points to Contents/MonoBundle instead, so the raw
	// test data files would not be found there.
	public static string BasePath => Foundation.NSBundle.MainBundle.ResourcePath ?? AppContext.BaseDirectory;
	public static bool IsPackagedApp => true;

	public static string FullAppPackageFilePath => BasePath;
#else
	public static string BasePath => AppContext.BaseDirectory;
	public static bool IsPackagedApp => false;

	public static string FullAppPackageFilePath => BasePath;
#endif
}
