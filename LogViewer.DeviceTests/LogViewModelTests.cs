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


		await Task.Delay(200);

		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(3110));
		Assert.That(reader.FilePath.Contains(vm.FileName));
	}

	[Test]
	public async Task TestSimpleFilter()
	{
		using var reader = GenerateLoggerReader();
		var vm = new ViewModels.LogViewModel(reader)
		{
			FileName = "app_lorem_ipsum",
			Query = "info"
		};

		await vm.FilterCommand.ExecuteAsync(null);
		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(668));
	}

	[Test]
	public async Task TestFilterWithOr()
	{
		using var reader = GenerateLoggerReader();
		var vm = new ViewModels.LogViewModel(reader)
		{
			FileName = "app_lorem_ipsum",
			Query = "info | debug"
		};
		await vm.FilterCommand.ExecuteAsync(null);
		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(3011));
	}

	[Test]
	public async Task TestFilterNot()
	{
		using var reader = GenerateLoggerReader();
		var vm = new ViewModels.LogViewModel(reader)
		{
			FileName = "app_lorem_ipsum",
			Query = "!info"
		};

		await vm.FilterCommand.ExecuteAsync(null);
		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(2442));
	}

	[Test]
	public async Task TestComplexFilter()
	{
		using var reader = GenerateLoggerReader();
		var vm = new ViewModels.LogViewModel(reader)
		{
			FileName = "app_lorem_ipsum",
			Query = "!info & error"
		};

		await vm.FilterCommand.ExecuteAsync(null);
		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(21));
	}

	[Test]
	public async Task FilterWithAnd()
	{
		using var reader = GenerateLoggerReader();
		var vm = new ViewModels.LogViewModel(reader)
		{
			FileName = "app_lorem_ipsum",
			Query = "info & error"
		};

		await vm.FilterCommand.ExecuteAsync(null);
		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(0));
	}

	static LoggerReader GenerateLoggerReader()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);
		return reader;
	}
}
