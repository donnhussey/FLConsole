namespace flconsole;

public sealed class ConsoleOutputBuffer(int MaxLines = 500)
{
    private readonly List<string> _lines = [];

    public void AddLine(string text)
    {
        _lines.Add(text);
        TrimToMaxLines();
    }

    public void AppendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var segments = normalizedText.Split('\n');

        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
        }

        _lines[^1] += segments[0];

        for (var index = 1; index < segments.Length; index++)
        {
            _lines.Add(segments[index]);
        }

        TrimToMaxLines();
    }

    public IReadOnlyList<string> GetVisibleLines(int maxVisibleLines)
    {
        var count = Math.Min(_lines.Count, Math.Max(0, maxVisibleLines));
        return _lines.TakeLast(count).ToList();
    }

    public IReadOnlyList<string> GetVisibleRows(int maxVisibleRows, int rowWidth)
    {
        if (maxVisibleRows <= 0)
        {
            return [];
        }

        var wrappedRows = _lines
            .SelectMany(line => WrapLine(line, rowWidth))
            .ToList();

        var count = Math.Min(wrappedRows.Count, maxVisibleRows);
        return wrappedRows.TakeLast(count).ToList();
    }

    private void TrimToMaxLines()
    {
        while (_lines.Count > MaxLines)
        {
            _lines.RemoveAt(0);
        }
    }

    private static IEnumerable<string> WrapLine(string line, int rowWidth)
    {
        var width = Math.Max(1, rowWidth);
        if (line.Length == 0)
        {
            yield return string.Empty;
            yield break;
        }

        for (var index = 0; index < line.Length; index += width)
        {
            var length = Math.Min(width, line.Length - index);
            yield return line.Substring(index, length);
        }
    }
}
