using System.Text;

namespace LogViewer;

public sealed record LogInfo(ReadOnlyMemory<byte> LineBytes)
{
	public string Text => field ??= Encoding.UTF8.GetString(LineBytes.Span);
}