using LogViewer.Parsers;
using LogViewer.ViewModels;
using System.Diagnostics;

namespace LogViewer.Managers;

static class FileManager
{
	static ContentPage CurrentPage => (ContentPage)Shell.Current.CurrentPage;

	public static void OpenFile(FileResult fileResult)
	{
		Assert(fileResult is not null);

		var reader = new LoggerReader(fileResult.FullPath);
		AddNewFileOnPage(fileResult, reader);
	}

	public static void CloseFile(LogViewModel vm, LoggerReader reader)
	{
		RemoveLogView(vm);
		reader.Dispose();
	}

	static void RemoveLogView(LogViewModel vm)
	{
		if (CurrentPage is not LogPage page)
		{
			return;
		}

		var tab = page.tabsLayout.Cast<TabView>().First(x => x.BindingContext == vm);
		page.RemoveLogView(tab);
	}

	static void AddNewFileOnPage(FileResult result, LoggerReader reader)
	{
		if (CurrentPage is not LogPage page)
		{
			return;
		}

		var vm = new LogViewModel(reader) { FileName = result.FileName };

		var view = new LogView
		{
			BindingContext = vm
		};

		var tab = new TabView
		{
			BindingContext = vm,
			LogView = view
		};

		page.OpenLog(tab);
	}

	public static void OpenFileInSide(FileResult result)
	{
		var reader = new LoggerReader(result.FullPath);
		AddNewFileSideBySide(result, reader);
	}

	static void AddNewFileSideBySide(FileResult result, LoggerReader reader)
	{
		if (CurrentPage is not LogPage page)
		{
			return;
		}

		var vm = new LogViewModel(reader) { FileName = result.FileName };
		var logView = new LogView
		{
			BindingContext = vm
		};
		var tab = new TabView
		{
			BindingContext = vm,
			LogView = logView
		};

		page.OpenLogInSide(tab);
	}
}
