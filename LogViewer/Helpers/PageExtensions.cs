using System.Runtime.CompilerServices;

namespace LogViewer.Helpers;

public static class PageExtensions
{
	extension(LogPage page)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void HideAllTabs()
		{
			foreach (var tab in page.tabsLayout.Children.Cast<TabView>())
			{
				tab.LogView.IsVisible = false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddTabView(TabView tabView) =>
			page.tabsLayout.Add(tabView);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddLogView(LogView logView)
		{
			Grid.SetRow(logView, 1);
			page.mainLayout.Add(logView);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RemoveLogView(TabView tabView)
		{
			var logView = tabView.LogView;
			page.tabsLayout.Remove(tabView);

			if (!logView.IsSideBySide)
			{
				page.mainLayout.Remove(logView);
				return;
			}

			logView.RemoveFromParent();
		}

		public void OpenLogInSide(TabView tabView)
		{
			var visibleView = page.tabsLayout.Cast<TabView>().FirstOrDefault(x => x.IsVisible);

			var logView = tabView.LogView;
			page.AddTabView(tabView);
			if (visibleView is null)
			{
				page.AddLogView(logView);
				return;
			}

			var dock = visibleView.LogView.dock;
			dock.Add(logView);
			visibleView.LogView.IsSideBySide = logView.IsSideBySide = true;
		}
	}

	extension(View view)
	{
		public void RemoveFromParent()
		{
			var element = GetParentLayout(view);
			element.Remove(view);
		}

		public Layout GetParentLayout()
		{
			var parent = view.Parent;
			if (parent is Layout l)
			{
				return l;
			}

			while (parent is not Layout)
			{
				parent = parent.Parent;
			}

			return (Layout)parent;
		}
	}
}
