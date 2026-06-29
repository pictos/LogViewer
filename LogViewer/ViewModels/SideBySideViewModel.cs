using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace LogViewer.ViewModels;

public sealed partial class SideBySideViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string? Query { get; set; }

	public ObservableCollection<LogViewModel> LoggerViewModels { get; } = [];


	[RelayCommand]
	void GlobalFilter()
	{
		foreach (var vm in LoggerViewModels)
		{
			vm.GlobalQuery = Query;
			vm.FilterCommand.Execute(null);
		}
	}
}
