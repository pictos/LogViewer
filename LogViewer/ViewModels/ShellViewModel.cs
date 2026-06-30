using CommunityToolkit.Mvvm.Input;
using LogViewer.Managers;
using LogViewer.Pages;
using LogViewer.Popups;
using LogViewer.Services;

namespace LogViewer.ViewModels;

sealed partial class ShellViewModel : BaseViewModel
{
	readonly PickOptions options;
	readonly FilePickerFileType customFileType = new 
			(
				new Dictionary<DevicePlatform, IEnumerable<string>>
				{
					{DevicePlatform.WinUI, [".txt", ".log"] }
				}
			);

	public ShellViewModel()
	{
		options = new PickOptions()
		{
			PickerTitle = "Pick your log",
			FileTypes = customFileType
		};
	}

	[RelayCommand]
	async Task OpenFile()
	{
		var result = await FilePicker.Default.PickAsync(options);

		if (result is null)
		{
			return;
		}

		FileManager.OpenFile(result);
	}

	[RelayCommand]
	async Task OpenSideBySide()
	{
		var result = await FilePicker.Default.PickAsync(options);

		if (result is null)
		{
			return;
		}

		FileManager.OpenFileInSide(result);
	}

	[RelayCommand]
	Task About() => NavigationService.ShowPopupAsync(new PopupPage(new AboutPopup()));

	[RelayCommand]
	Task GiveFeedback() => Browser.OpenAsync("https://github.com/pictos/LogViewer/issues/new/choose");
}
