
using LogViewer.Parsers;
using LogViewer.ViewModels;
using System.Diagnostics;

namespace LogViewer.Managers;

static class FileManager
{
	static readonly Dictionary<FileResult, LoggerReader> readers = [];

	static ContentPage CurrentPage => (ContentPage)Shell.Current.CurrentPage;

	public static void OpenFile(FileResult fileResult)
	{
		Debug.Assert(fileResult is not null);
		var reader = new LoggerReader(fileResult.FullPath);
		readers.TryAdd(fileResult, reader);
		AddNewFileOnPage(fileResult, reader);
	}

	static void AddNewFileOnPage(FileResult result, LoggerReader reader)
	{
		if (CurrentPage is not LogPage page)
		{
			return;
		}

		var vm = new LogViewModel { FileName = result.FileName };
		var tab = new TabView
		{
			BindingContext = vm
		};


		page.tabsLayout.Add(tab);
	}
}
