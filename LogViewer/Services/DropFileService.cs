using System.Runtime.CompilerServices;

namespace LogViewer.Services;

static partial class DropFileService
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsSupportedFile(string fileExtension) =>
        fileExtension.Equals(".txt", StringComparison.InvariantCultureIgnoreCase)
        || fileExtension.Equals(".log", StringComparison.InvariantCultureIgnoreCase)
        ||fileExtension.Equals("txt", StringComparison.InvariantCultureIgnoreCase)
        || fileExtension.Equals("log", StringComparison.InvariantCultureIgnoreCase);

}