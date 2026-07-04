namespace LogViewer.Services;

static partial class FilePickerService
{
    public static Task<FileResult?> PickAsync(PickOptions options)
        => FilePicker.Default.PickAsync(options)!;
}
