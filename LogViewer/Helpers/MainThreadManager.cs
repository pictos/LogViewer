namespace LogViewer.Helpers;

public static class MainThreadManager
{
	public static PJ.Core.UIThreadManager MainThreadSwitcher => field ??= new PJ.Core.UIThreadManager(SynchronizationContext.Current!);
}
