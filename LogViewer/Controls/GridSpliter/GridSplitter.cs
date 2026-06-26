using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LogViewer.Controls;

// from https://github.com/jsuarezruiz/TemplateUI/blob/master/src/TemplateUI/Controls/GridSplitter/GridSplitter.cs
public sealed partial class GridSplitter : TemplatedView
{
	const string ElementGridSplitter = "PART_GridSplitter";

	Grid? gridSplitter;

	double previousPositionX;
	double previousPositionY;
	PanGestureRecognizer panGesture = new();


	public static readonly BindableProperty ElementProperty =
		BindableProperty.Create(nameof(Element), typeof(View), typeof(GridSplitter), null);

	public View Element
	{
		get => (View)GetValue(ElementProperty);
		set => SetValue(ElementProperty, value);
	}


	public static readonly BindableProperty ResizeDirectionProperty =
		BindableProperty.Create(nameof(ResizeDirection), typeof(GridResizeDirection), typeof(GridSplitter), GridResizeDirection.Auto);

	public GridResizeDirection ResizeDirection
	{
		get => (GridResizeDirection)GetValue(ResizeDirectionProperty);
		set => SetValue(ResizeDirectionProperty, value);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		gridSplitter = (Grid)GetTemplateChild(ElementGridSplitter);

		Debug.Assert(gridSplitter is not null);

		UpdateIsEnabled();
	}

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == nameof(ResizeDirection))
		{
			UpdateLayout();
		}
		else if (propertyName == nameof(IsEnabled))
		{
			UpdateIsEnabled();
		}
	}

	void UpdateIsEnabled()
	{
		if (gridSplitter is null)
		{
			return;
		}

		if (IsEnabled)
		{
			panGesture.PanUpdated += OnPanUpdated;
			gridSplitter.GestureRecognizers.Add(panGesture);
		}
		else
		{
			panGesture.PanUpdated -= OnPanUpdated;
			gridSplitter.GestureRecognizers.Remove(panGesture);
		}
	}

	void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		switch (e.StatusType)
		{
			case GestureStatus.Running:
#if MACCATALYST
				var totalX = e.TotalX - previousPositionX;
				var totalY = e.TotalY - previousPositionY;
				UpdateLayout(totalX, totalY);

				previousPositionX = e.TotalX;
				previousPositionY = e.TotalY;
#endif
				UpdateLayout(e.TotalX, e.TotalY);
				break;
		}
	}

	void UpdateLayout(double offsetX = 0, double offsetY = 0)
	{
		if (Parent is not Grid)
		{
			return;
		}

		if (ResizeDirection == GridResizeDirection.Columns)
		{
			UpdateColumns(offsetX);
		}
		else
		{
			UpdateRows(offsetY);
		}
	}

	void UpdateRows(double offsetY)
	{
		if (offsetY is 0 || Parent is not Grid grid)
		{
			return;
		}

		var row = Grid.GetRow(this);
		var rowCount = grid.RowDefinitions.Count;

		if (rowCount <= 1 || row is 0 || row ==  rowCount - 1)
		{
			return;
		}

		var previousRow = grid.RowDefinitions[^1];

		double previousRowHeight;

		if (previousRow.Height.IsAbsolute)
		{
			previousRowHeight = previousRow.Height.Value;
		}
		else
		{
			previousRowHeight = UnsafeAccessorClass.GetUnsafeActualWidth(previousRow);
		}

		var actualHeight = previousRowHeight + offsetY;

		if (actualHeight < 0)
		{
			actualHeight = 0;
		}

		previousRow.Height = new(actualHeight);

	}

	void UpdateColumns(double offsetX)
	{
		if (offsetX is 0 || Parent is not Grid grid)
		{
			return;
		}

		var column = Grid.GetColumn(this);
		var columnCount = grid.ColumnDefinitions.Count;

		if (columnCount <= 1 || column is 0 || column >= columnCount - 1)
		{
			return;
		}

		var previousColumn = grid.ColumnDefinitions[^1];
		double previousRowWidth;

		if (previousColumn.Width.IsAbsolute)
		{
			previousRowWidth = previousColumn.Width.Value;
		}
		else
		{
			previousRowWidth = UnsafeAccessorClass.GetUnsafeActualWidth(previousColumn);
		}

		var actualWidth = previousRowWidth + offsetX;

		if (actualWidth < 0)
		{
			actualWidth = 0;
		}

		previousColumn.Width = new(actualWidth);
	}
}
