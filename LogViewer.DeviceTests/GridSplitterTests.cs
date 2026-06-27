using LogViewer.Controls;
using NUnit.Framework;
using System.Reflection;

namespace LogViewer.DeviceTests;

[TestFixture]
public class GridSplitterTests
{
	[Test]
	public void UpdateColumns_WithMainPageLikeLayout_ResizesOnlyAdjacentColumns()
	{
		var (grid, firstSplitter, _) = CreateMainPageLikeGrid();
		var left = grid.ColumnDefinitions[0];
		var middle = grid.ColumnDefinitions[2];
		var right = grid.ColumnDefinitions[4];

		InvokeNonPublic(firstSplitter, "UpdateColumns", 25d);

		Assert.That(left.Width.Value, Is.EqualTo(125d));
		Assert.That(middle.Width.Value, Is.EqualTo(75d));
		Assert.That(right.Width.Value, Is.EqualTo(100d));
	}

	[Test]
	public void UpdateColumns_WhenOffsetWouldMakeColumnNegative_DoesNotResize()
	{
		var (grid, firstSplitter, _) = CreateMainPageLikeGrid();
		var left = grid.ColumnDefinitions[0];
		var middle = grid.ColumnDefinitions[2];

		InvokeNonPublic(firstSplitter, "UpdateColumns", 150d);

		Assert.That(left.Width.Value, Is.EqualTo(100d));
		Assert.That(middle.Width.Value, Is.EqualTo(100d));
	}

	[Test]
	public void UpdateRows_WhenDirectionIsRows_ResizesAdjacentRows()
	{
		var grid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(90, GridUnitType.Absolute) },
				new RowDefinition { Height = new GridLength(10, GridUnitType.Absolute) },
				new RowDefinition { Height = new GridLength(90, GridUnitType.Absolute) }
			}
		};

		var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Rows };
		Grid.SetRow(splitter, 1);
		grid.Add(splitter);

		InvokeNonPublic(splitter, "UpdateRows", 30d);

		Assert.That(grid.RowDefinitions[0].Height.Value, Is.EqualTo(120d));
		Assert.That(grid.RowDefinitions[2].Height.Value, Is.EqualTo(60d));
	}

	static (Grid grid, GridSplitter firstSplitter, GridSplitter secondSplitter) CreateMainPageLikeGrid()
	{
		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) },
				new ColumnDefinition { Width = new GridLength(10, GridUnitType.Absolute) },
				new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) },
				new ColumnDefinition { Width = new GridLength(10, GridUnitType.Absolute) },
				new ColumnDefinition { Width = new GridLength(100, GridUnitType.Absolute) }
			}
		};

		var firstSplitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
		var secondSplitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };

		Grid.SetColumn(firstSplitter, 1);
		Grid.SetColumn(secondSplitter, 3);

		var leftPanel = new Grid();
		var middlePanel = new Grid();
		var rightPanel = new Grid();

		Grid.SetColumn(leftPanel, 0);
		Grid.SetColumn(middlePanel, 2);
		Grid.SetColumn(rightPanel, 4);

		grid.Add(leftPanel);
		grid.Add(middlePanel);
		grid.Add(rightPanel);
		grid.Add(firstSplitter);
		grid.Add(secondSplitter);

		return (grid, firstSplitter, secondSplitter);
	}

	static void InvokeNonPublic(object instance, string methodName, params object[] arguments)
	{
		var method = instance
			.GetType()
			.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(method, Is.Not.Null, $"Expected non-public method '{methodName}' was not found.");
		_ = method!.Invoke(instance, arguments);
	}
}
