using PJ.Gestures.Maui;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LogViewer.Controls;

// from https://github.com/jsuarezruiz/TemplateUI/blob/master/src/TemplateUI/Controls/GridSplitter/GridSplitter.cs
public sealed partial class GridSplitter : TemplatedView
{
	const string ElementGridSplitter = "PART_GridSplitter";

	Grid? gridSplitter;

	double previousTouchX;
	double previousTouchY;
	GestureBehavior gestureBehavior = new();


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
			gestureBehavior.Pan += OnPanUpdated;
			gridSplitter.Behaviors.Add(gestureBehavior);
		}
		else
		{
			gestureBehavior.Pan -= OnPanUpdated;
			gridSplitter.Behaviors.Remove(gestureBehavior);
		}
	}

	void OnPanUpdated(object? sender, PanEventArgs e)
	{
		switch (e.GestureStatus)
		{
			case GestureStatus.Running:
				var deltaX = e.Distance.X;
				var deltaY = e.Distance.Y;

				UpdateLayout(deltaX, deltaY);
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

		if (rowCount <= 1 || row is 0 || row >= rowCount - 1)
		{
			return;
		}

		var adjacentRow = grid.RowDefinitions[row + 1];

		double adjacentRowHeight;

		if (adjacentRow.Height.IsAbsolute)
		{
			adjacentRowHeight = adjacentRow.Height.Value;
		}
		else
		{
			adjacentRowHeight = GetAdjacentRowHeight(grid, row);
		}

		if (adjacentRowHeight <= 0)
		{
			return;
		}

		var actualHeight = adjacentRowHeight + offsetY;

		if (actualHeight < 0)
		{
			actualHeight = 0;
		}

		adjacentRow.Height = new(actualHeight);
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

		var adjacentColumn = grid.ColumnDefinitions[column + 1];
		double adjacentColumnWidth;

		if (adjacentColumn.Width.IsAbsolute)
		{
			adjacentColumnWidth = adjacentColumn.Width.Value;
		}
		else
		{
			adjacentColumnWidth = GetAdjacentColumnWidth(grid, column);
		}

		if (adjacentColumnWidth <= 0)
		{
			return;
		}

		var actualWidth = adjacentColumnWidth - offsetX;

		if (actualWidth < 0)
		{
			actualWidth = 0;
		}

		adjacentColumn.Width = new(actualWidth);
	}

	/// <summary>
	/// Computes the adjacent column's actual width by finding the next splitter
	/// or the grid's right edge.
	/// </summary>
	double GetAdjacentColumnWidth(Grid grid, int splitterColumn)
	{
		var splitterRight = Bounds.X + Bounds.Width;

		// Find the next splitter or use the grid edge
		double nextBoundary = grid.Width;
		foreach (var child in grid.Children)
		{
			if (child is GridSplitter other && other != this)
			{
				var otherColumn = Grid.GetColumn(other);
				if (otherColumn > splitterColumn)
				{
					nextBoundary = other.Bounds.X;
					break;
				}
			}
		}

		return nextBoundary - splitterRight;
	}

	/// <summary>
	/// Computes the adjacent row's actual height by finding the next splitter
	/// or the grid's bottom edge.
	/// </summary>
	double GetAdjacentRowHeight(Grid grid, int splitterRow)
	{
		var splitterBottom = Bounds.Y + Bounds.Height;

		// Find the next splitter or use the grid edge
		double nextBoundary = grid.Height;
		foreach (var child in grid.Children)
		{
			if (child is GridSplitter other && other != this)
			{
				var otherRow = Grid.GetRow(other);
				if (otherRow > splitterRow)
				{
					nextBoundary = other.Bounds.Y;
					break;
				}
			}
		}

		return nextBoundary - splitterBottom;
	}
}