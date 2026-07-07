using Foundation;
using LogViewer.Managers;
using UIKit;

namespace LogViewer.Services;

static partial class DropFileService
{
    public static async Task HandleDragSideBySide(PlatformDropEventArgs eventArgs)
    {
        if (eventArgs.DropSession.Items is not UIDragItem[] items || items.Length is 0)
        {
            return;
        }

        var loads = StartLoads(items);
        foreach (var load in loads)
        {
            var file = await load;
            if (file is not null)
            {
                FileManager.OpenFileInSide(file);
            }
        }
    }

    public static async Task HandleDragNewTab(PlatformDropEventArgs eventArgs)
    {
        if (eventArgs.DropSession.Items is not UIDragItem[] items || items.Length is 0)
        {
            return;
        }

        var loads = StartLoads(items);
        var openedFirst = false;
        foreach (var load in loads)
        {
            var file = await load;
            if (file is null)
            {
                continue;
            }

            if (!openedFirst)
            {
                FileManager.OpenFile(file);
                openedFirst = true;
            }
            else
            {
                FileManager.OpenFileInSide(file);
            }
        }
    }

    static Task<FileResult?>[] StartLoads(UIDragItem[] items)
    {
        var loads = new Task<FileResult?>[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            loads[i] = LoadFile(items[i].ItemProvider);
        }

        return loads;
    }

    static async Task<FileResult?> LoadFile(NSItemProvider provider)
    {
        var identifiers = provider.RegisteredTypeIdentifiers;

        // On macos is needed to start the loading up-front, as soon the `await`
        // freed the UIThread it will complete the event (async void) and Maui will
        // close the provider, causing an failure on next files to open
        var pending = new Task<NSObject>[identifiers.Length];
        for (var i = 0; i < identifiers.Length; i++)
        {
            
            pending[i] = provider.LoadItemAsync(identifiers[i], null);
        }

        foreach (var load in pending)
        {
            try
            {
                var data = await load.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

                if (data is NSUrl nsData && !string.IsNullOrEmpty(nsData.Path) && IsSupportedFile(nsData.PathExtension!))
                {
                    return new FileResult(nsData.Path);
                }
            }
            catch (NSErrorException)
            {
                // This representation couldn't be loaded (e.g. it's a promise or
                // an unsupported type). Try the next registered identifier.
            }
        }

        return null;
    }
}
