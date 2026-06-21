using CommunityToolkit.Mvvm.Input;

namespace LogViewer.ViewModels;

sealed partial class LogViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string? Status { get; set; }

	public required string FileName { get; init; }

	public LogViewModel()
	{
		
	}

	[RelayCommand]
	void Close()
	{

	}
}
