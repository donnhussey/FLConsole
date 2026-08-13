namespace flconsole.Console;

internal sealed class ConsoleOutputBuffer(int MaxLines = 500)
{
    private readonly Queue<string> _lines = new();
    private readonly StringBuilder _currentLine = new();
    private bool _hasCurrentLine;

    public void Clear()
    {
        _lines.Clear();
        _currentLine.Clear();
        _hasCurrentLine = false;
    }

    public void AddLine(string text)
    {
        FlushCurrentLine();
        _lines.Enqueue(text);
        TrimToMaxLines();
    }

    public void AppendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!_hasCurrentLine)
        {
            _hasCurrentLine = true;
        }

        foreach (var character in normalizedText)
        {
            if (character == '\r')
            {
                continue;
            }

            if (character == '\n')
            {
                FlushCurrentLine();
                _hasCurrentLine = true;
                continue;
            }

            _hasCurrentLine = true;
            _currentLine.Append(character);
        }

        TrimToMaxLines();
    }

    public IReadOnlyList<string> GetVisibleLines(int maxVisibleLines)
    {
        var count = Math.Min(_lines.Count + (_hasCurrentLine ? 1 : 0), Math.Max(0, maxVisibleLines));
        return GetLinesWithCurrent().TakeLast(count).ToList();
    }

    public IReadOnlyList<string> GetVisibleRows(int maxVisibleRows, int rowWidth)
    {
        if (maxVisibleRows <= 0)
        {
            return [];
        }

        var wrappedRows = GetLinesWithCurrent()
            .SelectMany(line => WrapLine(line, rowWidth))
            .ToList();

        var count = Math.Min(wrappedRows.Count, maxVisibleRows);
        return wrappedRows.TakeLast(count).ToList();
    }

    private void TrimToMaxLines()
    {
        while (_lines.Count > MaxLines)
        {
            _lines.Dequeue();
        }
    }

    private void FlushCurrentLine()
    {
        if (!_hasCurrentLine)
        {
            return;
        }

        _lines.Enqueue(_currentLine.ToString());
        _currentLine.Clear();
        _hasCurrentLine = false;
    }

    private IEnumerable<string> GetLinesWithCurrent()
    {
        foreach (var line in _lines)
        {
            yield return line;
        }

        if (_hasCurrentLine)
        {
            yield return _currentLine.ToString();
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

        var remaining = line;
        while (remaining.Length > width)
        {
            var wrapIndex = FindWrapIndex(remaining, width);
            if (wrapIndex <= 0)
            {
                wrapIndex = width;
            }

            yield return remaining[..wrapIndex];

            var nextIndex = wrapIndex;
            while (nextIndex < remaining.Length && char.IsWhiteSpace(remaining[nextIndex]))
            {
                nextIndex++;
            }

            remaining = remaining[nextIndex..];
        }

        yield return remaining;
    }

    private static int FindWrapIndex(string text, int width)
    {
        if (text.Length <= width)
        {
            return text.Length;
        }

        var searchLength = Math.Min(width + 1, text.Length);
        for (var index = searchLength - 1; index >= 0; index--)
        {
            if (!char.IsWhiteSpace(text[index]))
            {
                continue;
            }

            if (index == width)
            {
                return width;
            }

            if (index > 0)
            {
                return index;
            }
        }

        return width;
    }
}
