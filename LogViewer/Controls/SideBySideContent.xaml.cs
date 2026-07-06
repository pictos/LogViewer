using LogViewer.Services;
using LogViewer.ViewModels;

namespace LogViewer.Controls;

public partial class SideBySideContent
{
	const double SplitterWidth = 5;
	static readonly Color gridSplitterColor = Color.FromArgb("#6F6F6F");
	public SideBySideViewModel ViewModel { get; } = new();

	public SideBySideContent()
	{
		InitializeComponent();
		BindingContext = ViewModel;
	}

	public int LogViewCount => columns.Children.Count(static c => c is LogView);

	public void AddLog(LogView logView)
	{
		logView.Group = this;
		var vm = (LogViewModel)logView.BindingContext;
		ViewModel.LoggerViewModels.Add(vm);

		if (LogViewCount is 0)
		{
			logView.IsMain = true;
			columns.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
			Grid.SetColumn(logView, 0);
			columns.Add(logView);
		}
		else
		{
			var splitter = new GridSplitter
			{
				ResizeDirection = GridResizeDirection.Columns,
				Background = gridSplitterColor,
			};

			splitter.SetDynamicResource(VisualElement.StyleProperty, "GridSplitterStyle");
			var splitterColumn = columns.ColumnDefinitions.Count;
			columns.ColumnDefinitions.Add(new ColumnDefinition(SplitterWidth));
			columns.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			Grid.SetColumn(splitter, splitterColumn);
			Grid.SetColumn(logView, splitterColumn + 1);
			columns.Add(splitter);
			columns.Add(logView);
		}

		UpdateGlobalFilterVisibility();
	}

	public void RemoveLog(LogView logView)
	{
		var vm = (LogViewModel)logView.BindingContext;
		ViewModel.LoggerViewModels.Remove(vm);

		var index = columns.Children.IndexOf(logView);
		columns.Children.Remove(logView);

		if (index > 0 && columns.Children[index - 1] is GridSplitter splitter)
		{
			columns.Children.Remove(splitter);
		}

		RebuildColumns();
		UpdateGlobalFilterVisibility();
	}

	void RebuildColumns()
	{
		columns.ColumnDefinitions.Clear();
		var column = 0;
		foreach (var child in columns.Children)
		{
			columns.ColumnDefinitions.Add(child is GridSplitter
				? new ColumnDefinition(SplitterWidth)
				: new ColumnDefinition(GridLength.Star));
			SetColumn((BindableObject)child, column);
			column++;
		}
	}

	void UpdateGlobalFilterVisibility() => globalFilter.IsVisible = LogViewCount > 1;

	private async void DropGestureRecognizer_Drop(object sender, DropEventArgs e)
	{
		if (e.PlatformArgs is not PlatformDropEventArgs dropArgs)
		{
			return;
		}
		e.Handled = true;
		await DropFileService.HandleDragSideBySide(dropArgs);
	}
}