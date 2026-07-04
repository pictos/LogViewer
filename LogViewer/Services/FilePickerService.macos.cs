using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using UIKit;
using UniformTypeIdentifiers;

namespace LogViewer.Services;

// Maui(?) bug: macCatalyst implementation. Uses NSOpenPanel via ObjC runtime — the native
// macOS "Open File" dialog — because UIDocumentPickerViewController always fires
// WasCancelled when presented from MAUI's sheet-style window on macCatalyst
// (UIKit/NSOpenPanel lifecycle mismatch, present on all macCatalyst versions).
public static class FilePickerService
{
    // Needed to call NSOpenPanel's factory method and runModal, which are not
    // accessible through NSObject KVC.
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern nint NInt_objc_msgSend(IntPtr receiver, IntPtr selector);

    static UIDocumentPickerViewController? activePicker;
    static PickerDelegate? pickerDelegate;
    
    public static async Task<FileResult?> PickAsync(PickOptions options)
    {
        var tcs = new TaskCompletionSource<FileResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        await MainThreadSwitcher.SwitchToMainThreadAsync();

        if (!TryPickFileWithNSOpenPanel(tcs))
        {
            PickWithDocumentPicker(tcs);
        }

        return await tcs.Task;
    }

    static bool TryPickFileWithNSOpenPanel(TaskCompletionSource<FileResult?> tcs)
    {
        try
        {
            var panelClass = Class.GetHandle("NSOpenPanel");
            if (panelClass == IntPtr.Zero)
            {
                return false;
            }

            var panelHandle = IntPtr_objc_msgSend(panelClass, Selector.GetHandle("openPanel"));
            if (panelHandle == IntPtr.Zero)
            {
                return false;
            }

            var panel = ObjCRuntime.Runtime.GetNSObject(panelHandle)!;
            panel.SetValueForKey(new NSNumber(true), new NSString("canChooseFiles"));
            panel.SetValueForKey(new NSNumber(false), new NSString("canChooseDirectories"));
            panel.SetValueForKey(new NSNumber(false), new NSString("allowsMultipleSelection"));

            var types = new NSMutableArray(3);
            types.Add(UTTypes.PlainText);
            types.Add(UTTypes.Text);
            types.Add(UTTypes.Log);
            panel.SetValueForKey(types, new NSString("allowedContentTypes"));

            nint result = NInt_objc_msgSend(panelHandle, Selector.GetHandle("runModal"));

            if (result is not 1)
            {
                tcs.SetResult(null);
            }
            else
            {
                if (panel.ValueForKey(new NSString("URLs")) is NSArray urlsArray)
                {
                    var urls = NSArray.ArrayFromHandle<NSUrl>(urlsArray.Handle);
                    var url = urls?.FirstOrDefault();
                    tcs.SetResult(url is not null ? CopyToSandbox(url) : null);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            }

            return true;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    static void PickWithDocumentPicker(TaskCompletionSource<FileResult?> tcs)
    {
        UTType[] contentTypes =
        [
            UTTypes.PlainText,
            UTTypes.Utf8PlainText,
            UTTypes.Text,
            UTTypes.Log,
        ];

        var picker = new UIDocumentPickerViewController(contentTypes, asCopy: true)
        {
            AllowsMultipleSelection = false
        };

        Cleanup();
        
        activePicker = picker;
        pickerDelegate = new PickerDelegate(tcs, Cleanup);
        picker.Delegate = pickerDelegate;

        var parentVc = Platform.GetCurrentUIViewController();

        if (parentVc is null)
        {
            Cleanup();
            tcs.TrySetResult(null);
            return;
        }
        
        parentVc.PresentViewController(picker, true, null);

        void Cleanup()
        {
            activePicker?.Dispose();
            activePicker = null;
            pickerDelegate?.Dispose();
            pickerDelegate = null;
        }
    }

    static FileResult? CopyToSandbox(NSUrl securityScopedUrl)
    {
        var accessed = securityScopedUrl.StartAccessingSecurityScopedResource();

        try
        {
            if (securityScopedUrl.Path is not string sourcePath)
            {
                return null;
            }

            var fileName = Path.GetFileName(sourcePath);
            var destinationPath = Path.Combine(Path.GetTempPath(), fileName);
            File.Copy(sourcePath, destinationPath, true);
            return new(destinationPath)
            {
                FileName = fileName
            };
        }
        finally
        {
            if (accessed)
            {
                securityScopedUrl.StopAccessingSecurityScopedResource();
            }
        }
    }
    
    
    // IF some day the maui impl. doesn't work to get the topmost VC...
    // static UIViewController? GetTopmostViewController()
    // {
    //     var scene = UIApplication.SharedApplication.ConnectedScenes
    //         .OfType<UIWindowScene>()
    //         .FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive);
    //
    //     var window = scene?.Windows.FirstOrDefault(w => w.IsKeyWindow)
    //                  ?? scene?.Windows.FirstOrDefault();
    //
    //     var root = window?.RootViewController;
    //     if (root is null)
    //         return null;
    //
    //     var topmost = root;
    //     while (topmost.PresentedViewController is { } presented)
    //         topmost = presented;
    //
    //     return topmost;
    // }
}

sealed class PickerDelegate(TaskCompletionSource<FileResult?> tcs, Action cleanup) : UIDocumentPickerDelegate
{
    public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
    {
        cleanup();
        tcs.TrySetResult(url?.Path is string path ? new(path) : null);
    }

    public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
    {
        cleanup();
        var url = urls.FirstOrDefault();
        tcs.TrySetResult(url?.Path is string path ? new(path) : null);
    }

    public override void WasCancelled(UIDocumentPickerViewController controller)
    {
        cleanup();
        tcs.TrySetResult(null);
    }
}