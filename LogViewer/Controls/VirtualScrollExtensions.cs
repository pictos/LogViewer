#if WINDOWS
using Microsoft.UI.Xaml;
#endif
using Nalu;

namespace LogViewer.Controls;

public static class VirtualScrollExtensions
{
	public static readonly BindableProperty IsHorizontalScrollEnabledProperty =
		BindableProperty.CreateAttached("IsHorizontalScrollEnabled", typeof(bool), typeof(VirtualScroll), false);

	public static void SetIsHorizontalScrollEnabled(VirtualScroll view, bool value)
	{
		view.SetValue(IsHorizontalScrollEnabledProperty, value);
	}

	public static bool GetIsHorizontalScrollEnabled(BindableObject view)
	{
		return (bool)view.GetValue(IsHorizontalScrollEnabledProperty);
	}

	public static void InitHandler()
	{
#if WINDOWS
		VirtualScrollHandler.Mapper.Add("IsHorizontalScrollEnabled", (handler, view) =>
		{
			if (view is not VirtualScroll virtualScroll)
			{
				return;
			}

			var isHorizontalScrollEnabled = virtualScroll.IsHorizontalScrollEnabled;
			var scrollViewer = UnsafeAccessorClass.GetScrollViewer(handler);
			if (isHorizontalScrollEnabled)
			{
				scrollViewer.HorizontalScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode.Enabled;
				scrollViewer.HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto;
			}
			else
			{
				scrollViewer.HorizontalScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode.Disabled;
				scrollViewer.HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Disabled;
			}
		});
#endif
	}

	extension(VirtualScroll scroll)
	{
		public bool IsHorizontalScrollEnabled
		{
			get => GetIsHorizontalScrollEnabled(scroll);
			set => SetIsHorizontalScrollEnabled(scroll, value);
		}
	}
}
