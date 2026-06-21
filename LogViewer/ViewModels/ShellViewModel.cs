using CommunityToolkit.Mvvm.Input;
using LogViewer.Managers;

namespace LogViewer.ViewModels;

sealed partial class ShellViewModel : BaseViewModel
{
	PickOptions? options;

	[RelayCommand]
	public async Task OpenFile()
	{
		var customFileType = new FilePickerFileType
			(
				new Dictionary<DevicePlatform, IEnumerable<string>>
				{
					{DevicePlatform.WinUI, [".txt", ".log"] }
				}
			);

		options ??= new PickOptions()
		{
			PickerTitle = "Pick your log",
			FileTypes = customFileType

		};

		var result = await FilePicker.Default.PickAsync(options);

		if (result is null)
		{
			return;
		}

		FileManager.OpenFile(result);
	}
}
