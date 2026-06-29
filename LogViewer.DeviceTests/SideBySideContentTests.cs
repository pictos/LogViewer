using LogViewer.Controls;
using LogViewer.Parsers;
using LogViewer.ViewModels;
using NUnit.Framework;

namespace LogViewer.DeviceTests;

[TestFixture]
public class SideBySideContentTests
{
	[Test]
	public void AddLog_First_BecomesMainWithSingleColumn()
	{
		var content = new SideBySideContent();

		content.AddLog(CreateLogView(out var vm));

		Assert.That(content.LogViewCount, Is.EqualTo(1));
		Assert.That(content.columns.ColumnDefinitions.Count, Is.EqualTo(1));
		Assert.That(content.ViewModel.LoggerViewModels, Does.Contain(vm));
		Assert.That(content.globalFilter.IsVisible, Is.False);
	}

	[Test]
	public void AddLog_Second_AddsSplitterColumnsAndShowsGlobalFilter()
	{
		var content = new SideBySideContent();

		var first = CreateLogView(out _);
		var second = CreateLogView(out _);
		content.AddLog(first);
		content.AddLog(second);

		Assert.That(content.LogViewCount, Is.EqualTo(2));
		Assert.That(content.columns.ColumnDefinitions.Count, Is.EqualTo(3));
		Assert.That(content.columns.Children.OfType<GridSplitter>().Count(), Is.EqualTo(1));
		Assert.That(first.IsMain, Is.True);
		Assert.That(second.IsMain, Is.False);
		Assert.That(content.globalFilter.IsVisible, Is.True);
	}

	[Test]
	public void AddLog_Third_ProducesFiveColumnsAndTwoSplitters()
	{
		var content = new SideBySideContent();

		content.AddLog(CreateLogView(out _));
		content.AddLog(CreateLogView(out _));
		content.AddLog(CreateLogView(out _));

		Assert.That(content.LogViewCount, Is.EqualTo(3));
		Assert.That(content.columns.ColumnDefinitions.Count, Is.EqualTo(5));
		Assert.That(content.columns.Children.OfType<GridSplitter>().Count(), Is.EqualTo(2));
	}

	[Test]
	public void RemoveLog_LastView_RemovesViewSplitterAndFixesColumns()
	{
		var content = new SideBySideContent();
		content.AddLog(CreateLogView(out _));
		content.AddLog(CreateLogView(out _));
		var third = CreateLogView(out var thirdVm);
		content.AddLog(third);

		content.RemoveLog(third);

		Assert.That(content.LogViewCount, Is.EqualTo(2));
		Assert.That(content.columns.ColumnDefinitions.Count, Is.EqualTo(3));
		Assert.That(content.columns.Children.OfType<GridSplitter>().Count(), Is.EqualTo(1));
		Assert.That(content.ViewModel.LoggerViewModels, Does.Not.Contain(thirdVm));
		Assert.That(content.globalFilter.IsVisible, Is.True);
	}

	[Test]
	public void RemoveLog_DownToSingleView_HidesGlobalFilter()
	{
		var content = new SideBySideContent();
		content.AddLog(CreateLogView(out _));
		var second = CreateLogView(out _);
		content.AddLog(second);

		content.RemoveLog(second);

		Assert.That(content.LogViewCount, Is.EqualTo(1));
		Assert.That(content.columns.ColumnDefinitions.Count, Is.EqualTo(1));
		Assert.That(content.globalFilter.IsVisible, Is.False);
	}

	[Test]
	public void GlobalFilter_PropagatesQueryToEachLogViewModel()
	{
		var content = new SideBySideContent();
		content.AddLog(CreateLogView(out var first));
		content.AddLog(CreateLogView(out var second));

		content.ViewModel.Query = "info";
		content.ViewModel.GlobalFilterCommand.Execute(null);

		Assert.That(first.GlobalQuery, Is.EqualTo("info"));
		Assert.That(second.GlobalQuery, Is.EqualTo("info"));
	}

	[TestCase(null, "info", 668)]
	[TestCase("error", "info", 0)]
	[TestCase("info", null, 668)]
	public async Task GlobalFilter_MergesWithInnerQuery(string? inner, string? global, int expectedCount)
	{
		using var reader = GenerateLoggerReader();
		var vm = new LogViewModel(reader)
		{
			FileName = "app_lorem_ipsum",
			Query = inner,
			GlobalQuery = global
		};

		await vm.FilterCommand.ExecuteAsync(null);

		Assert.That(vm.LogSource.GetItemCount(0), Is.EqualTo(expectedCount));
	}

	static LogView CreateLogView(out LogViewModel vm)
	{
		vm = new LogViewModel(GenerateLoggerReader()) { FileName = "app_lorem_ipsum" };
		return new LogView { BindingContext = vm };
	}

	static LoggerReader GenerateLoggerReader()
	{
		var source = Path.Combine(FullAppPackageFilePath, "app_lorem_ipsum.txt");
		var temp = Path.Combine(Path.GetTempPath(), $"sbs_{Guid.NewGuid():N}.txt");
		File.Copy(source, temp, overwrite: true);
		return new LoggerReader(temp);
	}
}
