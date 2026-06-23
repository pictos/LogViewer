using System.Text;

namespace LogViewer;

public sealed record LogInfo(ReadOnlyMemory<byte> LineBytes)
{
	public int Index { get; set; }

	public bool IsOdd => (Index & 1) == 1;

	public string Text => field ??= Encoding.UTF8.GetString(LineBytes.Span);
}