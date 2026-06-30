using LogViewer.Pages;

namespace LogViewer.Services;

static class NavigationService
{
	static Shell Shell => Shell.Current;

	static INavigation Navigation => Shell.Navigation;

	public static async Task ShowPopupAsync(PopupPage popup)
	{
		ArgumentNullException.ThrowIfNull(popup, nameof(popup));

		await RemovePopupAsync();
		await MainThreadSwitcher.SwitchToMainThreadAsync();
		await Navigation.PushModalAsync(popup, false);
	}

	public static async ValueTask RemovePopupAsync()
	{
		var modalStack = Shell.Navigation.ModalStack;
		if (modalStack.Count is 0)
		{
			return;
		}

		var popup = modalStack[^1];

		if (popup is not PopupPage)
		{
			return;
		}

		await MainThreadSwitcher.SwitchToMainThreadAsync();
		await Navigation.PopModalAsync(false).ConfigureAwait(false);
	}
}
