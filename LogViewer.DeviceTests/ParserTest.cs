using LogViewer.Parsers;
using NUnit.Framework;
#if WINDOWS
using Windows.ApplicationModel;
#endif

namespace LogViewer.DeviceTests;

[TestFixture]
public class ParserTest
{
#if WINDOWS
	// Code from Microsoft.Maui Appinfo.windows.cs
	static readonly Lazy<bool> _isPackagedAppLazy = new (() =>
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

	/// <summary>
	/// Gets if this app is a packaged app.
	/// </summary>
	public static bool IsPackagedApp => _isPackagedAppLazy.Value;
	static readonly Lazy<string> platformGetFullAppPackageFilePath = new Lazy<string>(() =>
	{
		return IsPackagedApp
			? Package.Current.InstalledLocation.Path
			: AppContext.BaseDirectory;
	});

	static string FullAppPackageFilePath => platformGetFullAppPackageFilePath.Value;
#else
// TODO Implement maccatalyst
#endif

	[Test]
	public void OpenAndParseTheLog()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);
		var result = reader.Process();

		Assert.That(result.Length, Is.EqualTo(3110));
	}

	[Test]
	public void TestFilter()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);
		reader.Process();

		var filter = reader.Filter("info");

		Assert.That(filter.Length, Is.EqualTo(668));
	}

	[Test]
	public void TestFilterCaseInsensitive()
	{

		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);
		reader.Process();

		var filter = reader.Filter("info");
		var filter2 = reader.Filter("Info");
		var filter3 = reader.Filter("iNFo");

		Assert.That(filter.Length, Is.EqualTo(filter2.Length));
		Assert.That(filter.Length, Is.EqualTo(filter3.Length));
	}

	[Test]
	public void Fail()
	{
		Assert.IsTrue(false);
	}
}
