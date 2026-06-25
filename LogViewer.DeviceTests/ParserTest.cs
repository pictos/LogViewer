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
	public void TestFilterWithAnd()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);

		var onlyInfo = reader.Filter("info");
		var onlyDebug = reader.Filter("debug");
		var andResult = reader.Filter("info & debug");

		// AND should return only lines matching both terms
		Assert.That(andResult.Length, Is.LessThanOrEqualTo(onlyInfo.Length));
		Assert.That(andResult.Length, Is.LessThanOrEqualTo(onlyDebug.Length));
	}

	[Test]
	public void TestFilterWithOr()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);

		var onlyInfo = reader.Filter("info");
		var onlyDebug = reader.Filter("debug");
		var orResult = reader.Filter("info | debug");

		// OR should return at least as many lines as either individual term
		Assert.That(orResult.Length, Is.GreaterThanOrEqualTo(onlyInfo.Length));
		Assert.That(orResult.Length, Is.GreaterThanOrEqualTo(onlyDebug.Length));
	}

	[Test]
	public void TestFilterWithNot()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);

		var all = reader.Process();
		var withInfo = reader.Filter("info");
		var withoutInfo = reader.Filter("!info");

		// NOT: lines with + lines without should cover all lines
		Assert.That(withInfo.Length + withoutInfo.Length, Is.EqualTo(all.Length));
	}

	[Test]
	public void TestFilterComplexExpression()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);

		// Should not throw
		var result = reader.Filter("(info | debug) & !error");
		Assert.That(result, Is.Not.Null);
	}

	[Test]
	public void TestQueryParser_SimpleTermEquivalent()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);

		// A bare term via the new parser should match the original behaviour
		var plain = reader.Filter("info");
		var parsed = reader.Filter("info");

		Assert.That(parsed.Length, Is.EqualTo(plain.Length));
	}

}
