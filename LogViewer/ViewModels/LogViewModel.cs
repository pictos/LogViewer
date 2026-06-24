using CommunityToolkit.Mvvm.Input;
using LogViewer.Managers;
using LogViewer.Parsers;
using Nalu;
using System.Diagnostics;

namespace LogViewer.ViewModels;

sealed partial class LogViewModel : BaseViewModel
{
	readonly LoggerReader reader;
	IVirtualScrollAdapter? fullText;

	[ObservableProperty]
	public partial IVirtualScrollAdapter LogSource { get; private set; }

	[ObservableProperty]
	public partial string? Status { get; set; }

	[ObservableProperty]
	public partial string? Query { get; set; }

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
		await MainThreadSwitcher.SwitchToMainThreadAsync();
		fullText = LogSource = new ImmutableArrayAdapter(result);
		Status = $"Loaded log with {result.Length} lines.";
	}

	[RelayCommand]
	void Filter()
	{
		var q = Query;
		if (string.IsNullOrEmpty(q))
		{
			Debug.Assert(fullText is not null);
			LogSource = fullText;
			Status = $"Full log with {fullText.GetItemCount(0)} lines.";
			return;
		}

		var result = reader.Filter(q);
		LogSource = new ImmutableArrayAdapter(result);
		Status = $"Filter applied, found {result.Length} lines.";
	}

	[RelayCommand]
	void Close()
	{
		FileManager.CloseFile(this, reader);
	}
}
