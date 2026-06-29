using System.Runtime.CompilerServices;

namespace LogViewer.Helpers;

public static class PageExtensions
{
	extension(LogPage page)
	{
		public void HideAllGroups()
		{
			foreach (var group in page.mainLayout.Children.OfType<SideBySideContent>())
			{
				group.IsVisible = false;
			}
		}

		public void ShowGroup(SideBySideContent group)
		{
			page.HideAllGroups();
			group.IsVisible = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddTabView(TabView tabView) =>
			page.tabsLayout.Add(tabView);

		public void OpenLog(TabView tabView)
		{
			var group = new SideBySideContent();
			Grid.SetRow(group, 1);
			page.mainLayout.Add(group);

			page.AddTabView(tabView);
			group.AddLog(tabView.LogView);
			page.ShowGroup(group);
		}

		public void OpenLogInSide(TabView tabView)
		{
			var group = page.mainLayout.Children.OfType<SideBySideContent>().FirstOrDefault(g => g.IsVisible);
			if (group is null)
			{
				page.OpenLog(tabView);
				return;
			}

			page.AddTabView(tabView);
			group.AddLog(tabView.LogView);
		}

		public void RemoveLogView(TabView tabView)
		{
			var logView = tabView.LogView;
			var group = logView.Group;

			if (group is null || logView.IsMain)
			{
				page.CloseGroup(group);
				return;
			}

			group.RemoveLog(logView);
			page.tabsLayout.Remove(tabView);
		}

		void CloseGroup(SideBySideContent? group)
		{
			if (group is null)
			{
				return;
			}

			var tabs = page.tabsLayout.Children
				.OfType<TabView>()
				.Where(t => t.LogView.Group == group)
				.ToArray();

			foreach (var tab in tabs)
			{
				page.tabsLayout.Remove(tab);
			}

			page.mainLayout.Remove(group);
		}
	}
}
