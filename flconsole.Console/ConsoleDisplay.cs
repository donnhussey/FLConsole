namespace flconsole.Console;

internal sealed class ConsoleDisplay : IConsoleDisplay
{
    private readonly string _promptPrefix;
    private readonly IConsoleTerminal ConsoleTerminal;
    private readonly ConsoleOutputBuffer OutputBuffer;
    private readonly object _syncRoot = new();

    internal ConsoleDisplay(string promptPrefix, int maxLines, IConsoleTerminal consoleTerminal)
    {
        _promptPrefix = promptPrefix;
        ConsoleTerminal = consoleTerminal;
        OutputBuffer = new ConsoleOutputBuffer(maxLines);
    }

    public void Clear()
    {
        OutputBuffer.Clear();
        ConsoleTerminal.Clear();
    }

    private void RenderOutput()
    {
        lock (_syncRoot)
        {
            var promptRow = ConsoleTerminal.WindowHeight - 1;
            var maxVisibleLines = Math.Max(0, promptRow);
            var visibleLines = OutputBuffer.GetVisibleRows(maxVisibleLines, ConsoleTerminal.WindowWidth).ToList();

            for (var row = 0; row < promptRow; row++)
            {
                ConsoleTerminal.SetCursorPosition(0, row);
                ConsoleTerminal.Write(new string(' ', ConsoleTerminal.WindowWidth));
            }

            ConsoleTerminal.SetCursorPosition(0, 0);
            for (var index = 0; index < visibleLines.Count; index++)
            {
                ConsoleTerminal.Write(visibleLines[index]);
                if (index < visibleLines.Count - 1)
                {
                    ConsoleTerminal.SetCursorPosition(0, index + 1);
                }
            }
        }
    }

    public void AppendText(string text)
    {
        OutputBuffer.AppendText(text);
        RenderOutput();
    }

    public void AppendLine(string text)
    {
        OutputBuffer.AddLine(text);
        RenderOutput();
    }

    public void ShowPrompt(string promptText, int cursorIndex)
    {
        lock (_syncRoot)
        {
            var promptRow = ConsoleTerminal.WindowHeight - 1;
            ConsoleTerminal.SetCursorPosition(0, promptRow);
            ConsoleTerminal.Write(new string(' ', ConsoleTerminal.WindowWidth));

            ConsoleTerminal.SetCursorPosition(0, promptRow);
            ConsoleTerminal.Write(_promptPrefix + promptText);

            var cursorColumn = _promptPrefix.Length + Math.Min(cursorIndex, promptText.Length);
            ConsoleTerminal.SetCursorPosition(Math.Min(cursorColumn, ConsoleTerminal.WindowWidth - 1), promptRow);
        }
    }
}
