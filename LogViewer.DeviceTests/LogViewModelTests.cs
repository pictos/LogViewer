using LogViewer.Parsers;
using NUnit.Framework;

namespace LogViewer.DeviceTests;

[TestFixture]
public class LogViewModelTests
{
	[Test]
	public async Task TestVmInit()
	{
		using var reader = GenerateLoggerReader();
		var vm = new ViewModels.LogViewModel(reader)
		{
			FileName = "app_lorem_ipsum"
		};

		// Wait for the async initialization to complete
		// It runs inside the ctor as a FireAndForget, 200ms is more than enough
		// to everything be in place.
		await Task.Delay(200);

		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(3110));
		Assert.That(reader.FilePath.Contains(vm.FileName));
	}

	[TestCase("info", 668)]
	[TestCase("info | debug", 3011)]
	[TestCase("!info", 2442)]
	[TestCase("!info & error", 21)]
	[TestCase("info & error", 0)]
	public async Task TestFilter(string query, int expectedCount)
	{
		using var reader = GenerateLoggerReader();
		var vm = new ViewModels.LogViewModel(reader)
		{
			FileName = "app_lorem_ipsum",
			Query = query
		};
		await vm.FilterCommand.ExecuteAsync(null);
		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(expectedCount));
	}

	static LoggerReader GenerateLoggerReader()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);
		return reader;
	}
}
