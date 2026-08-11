namespace flconsole;

public sealed class ConsoleRenderer(ConsoleUiSettings Settings, IConsoleFacade ConsoleFacade) : IRenderer
{
    private readonly object _syncRoot = new();

    public void Clear()
    {
        ConsoleFacade.Clear();
    }

    public void RenderOutput(ConsoleOutputBuffer outputBuffer)
    {
        lock (_syncRoot)
        {
            var promptRow = ConsoleFacade.WindowHeight - 1;
            var maxVisibleLines = Math.Max(0, promptRow);
            var visibleLines = outputBuffer.GetVisibleRows(maxVisibleLines, ConsoleFacade.WindowWidth).ToList();

            for (var row = 0; row < promptRow; row++)
            {
                ConsoleFacade.SetCursorPosition(0, row);
                ConsoleFacade.Write(new string(' ', ConsoleFacade.WindowWidth));
            }

            ConsoleFacade.SetCursorPosition(0, 0);
            for (var index = 0; index < visibleLines.Count; index++)
            {
                ConsoleFacade.Write(visibleLines[index]);
                if (index < visibleLines.Count - 1)
                {
                    ConsoleFacade.SetCursorPosition(0, index + 1);
                }
            }
        }
    }

    public void RenderInput(string promptText, int cursorIndex)
    {
        lock (_syncRoot)
        {
            var promptRow = ConsoleFacade.WindowHeight - 1;
            ConsoleFacade.SetCursorPosition(0, promptRow);
            ConsoleFacade.Write(new string(' ', ConsoleFacade.WindowWidth));

            ConsoleFacade.SetCursorPosition(0, promptRow);
            ConsoleFacade.Write(Settings.PromptPrefix + promptText);

            var cursorColumn = Settings.PromptPrefix.Length + Math.Min(cursorIndex, promptText.Length);
            ConsoleFacade.SetCursorPosition(Math.Min(cursorColumn, ConsoleFacade.WindowWidth - 1), promptRow);
        }
    }
}
