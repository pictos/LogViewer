using LogViewer.Parsers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogViewer.DeviceTests;


[TestFixture]
public class LogViewModelTests
{
	[Test]
	public void TestVmInit()
	{
		using var reader = GenerateLoggerReader();
		var vm = new ViewModels.LogViewModel(reader)
		{
			FileName= "app_lorem_ipsum"
		};

		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(3110));
		Assert.That(reader.FilePath.Contains(vm.FileName));
	}

	static LoggerReader GenerateLoggerReader()
	{
		var path = FullAppPackageFilePath;
		var filePath = Path.Combine(path, "app_lorem_ipsum.txt");
		using var reader = new LoggerReader(filePath);
		return reader;
	}
}
