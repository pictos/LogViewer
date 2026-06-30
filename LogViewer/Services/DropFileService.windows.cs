using LogViewer.Managers;
using Microsoft.UI.Xaml;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace LogViewer.Services;

static class DropFileService
{
	public static async Task HandleDragSideBySide(PlatformDropEventArgs eventArgs)
	{
		if (eventArgs.DragEventArgs is not Microsoft.UI.Xaml.DragEventArgs wArgs)
		{
			return;
		}

		var (draggedItems, dragUI) = await GetDesiredItems(wArgs);

		if (draggedItems.Count is 0)
		{
			return;
		}

		dragUI.Caption = "Open";
		foreach (var item in draggedItems)
		{
			if (item is not Windows.Storage.StorageFile { FileType: string fileExtension } file)
			{
				continue;
			}

			if (!IsSupportedFile(fileExtension))
			{
				dragUI.Caption = "Invalid file will be ignored.";
				continue;
			}

			var fileResult = new FileResult(file.Path);
			FileManager.OpenFileInSide(fileResult);
		}
	}

	public static async Task HandleDragNewTab(PlatformDropEventArgs eventArgs)
	{
		if (eventArgs.DragEventArgs is not Microsoft.UI.Xaml.DragEventArgs wArgs)
		{
			return;
		}

		var (draggedItems, dragUI) = await GetDesiredItems(wArgs);

		if (draggedItems.Count is 0)
		{
			return;
		}

		dragUI.Caption = "Open";
		OpenFile(draggedItems, dragUI);

		if (draggedItems.Count is 1)
		{
			return;
		}

		for (var i = 1; i < draggedItems.Count; i++)
		{
			var item = draggedItems[i];
			if (item is not Windows.Storage.StorageFile { FileType: string fileExtension } file)
			{
				continue;
			}

			if (!IsSupportedFile(fileExtension))
			{
				dragUI.Caption = "Invalid file will be ignored.";
				continue;
			}

			var fileResult = new FileResult(file.Path);
			FileManager.OpenFileInSide(fileResult);
		}
	}

	private static void OpenFile(IReadOnlyList<IStorageItem> draggedItems, DragUIOverride dragUI)
	{
		var item = draggedItems[0];
		if (item is not Windows.Storage.StorageFile { FileType: string fileExtension } file)
		{
			dragUI.Caption = "Invalid file type!";
			return;
		}

		if (!IsSupportedFile(fileExtension))
		{
			dragUI.Caption = "Invalid file will be ignored.";
			return;
		}

		var fileResult = new FileResult(file.Path);
		FileManager.OpenFile(fileResult);
	}

	static async Task<(IReadOnlyList<IStorageItem> files, DragUIOverride dragUI)> GetDesiredItems(Microsoft.UI.Xaml.DragEventArgs wArgs)
	{
		var dragUI = wArgs.DragUIOverride;

		var files = await wArgs.DataView.GetStorageItemsAsync();

		return (files, dragUI);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool IsSupportedFile(string fileName) =>
		fileName.Equals(".txt", StringComparison.InvariantCultureIgnoreCase) || fileName.Equals(".log", StringComparison.InvariantCultureIgnoreCase);
}
