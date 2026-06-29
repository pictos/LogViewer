using CommunityToolkit.Mvvm.Input;
using LogViewer.Managers;
using LogViewer.Parsers;
using Nalu;
using System.Diagnostics;

namespace LogViewer.ViewModels;

public sealed partial class LogViewModel : BaseViewModel
{
	readonly LoggerReader reader;
	IVirtualScrollAdapter? fullText;

	[ObservableProperty]
	public partial IVirtualScrollAdapter LogSource { get; private set; }

	[ObservableProperty]
	public partial string? Status { get; set; }

	[ObservableProperty]
	public partial string? Query { get; set; }

	public string? GlobalQuery { get; set; }

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
	async Task Filter()
	{
		var q = BuildQuery();
		if (string.IsNullOrEmpty(q))
		{
			Debug.Assert(fullText is not null);
			LogSource = fullText;
			Status = $"Full log with {fullText.GetItemCount(0)} lines.";
			return;
		}

		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		try
		{
			var result = reader.Filter(q);
			await MainThreadSwitcher.SwitchToMainThreadAsync();
			LogSource = new ImmutableArrayAdapter(result);
			Status = $"Filter applied, found {result.Length} lines.";
		}
		catch (Exception ex)
		{
			Status = $"Error applying filter: {ex.Message}";
		}
	}

	string? BuildQuery()
	{
		var inner = Query;
		var global = GlobalQuery;
		var hasInner = !string.IsNullOrEmpty(inner);
		var hasGlobal = !string.IsNullOrEmpty(global);

		if (hasGlobal && hasInner)
		{
			return $"({global}) & ({inner})";
		}

		return hasGlobal ? global : inner;
	}

	[RelayCommand]
	void Close()
	{
		FileManager.CloseFile(this, reader);
	}
}
