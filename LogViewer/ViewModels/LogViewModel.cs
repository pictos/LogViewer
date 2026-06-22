using CommunityToolkit.Mvvm.Input;
using LogViewer.Parsers;
using Nalu;
using System.Collections.Immutable;
using System.Diagnostics;

namespace LogViewer.ViewModels;

sealed partial class LogViewModel : BaseViewModel
{
	readonly LoggerReader reader;

	[ObservableProperty]
	public partial IVirtualScrollAdapter LogSource { get; private set; }

	[ObservableProperty]
	public partial string? Status { get; set; }

	public required string FileName { get; init; }

	public LogViewModel(LoggerReader reader)
	{
		LogSource = default!;
		Debug.Assert(reader is not null);
		this.reader = reader;
		_ = ProcessAsync();
	}

	async Task ProcessAsync()
	{
		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		var result = reader.Process();
		await UIThreadManager.SwitchToMainThreadAsync();
		LogSource = new ImmutableArrayAdapter<string>(result);
	}

	[RelayCommand]
	void Close()
	{

	}
}
