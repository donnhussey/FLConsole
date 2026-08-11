namespace flconsole;

public sealed class ConsolePromptHandler(IRenderer Renderer, IConsoleInput ConsoleInput) : IPromptReader, IPromptState
{
    public string CurrentText { get; private set; } = string.Empty;

    public int CurrentCursorIndex { get; private set; }

    public bool IsActive { get; private set; }

    public void StartEditing()
    {
        IsActive = true;
        CurrentText = string.Empty;
        CurrentCursorIndex = 0;
        Renderer.RenderInput(CurrentText, CurrentCursorIndex);
    }

    public void UpdateState(string text, int cursorIndex)
    {
        CurrentText = text;
        CurrentCursorIndex = cursorIndex;
        Renderer.RenderInput(CurrentText, CurrentCursorIndex);
    }

    public void StopEditing()
    {
        IsActive = false;
        Renderer.RenderInput(CurrentText, CurrentCursorIndex);
    }

    public string? ReadLineFromPrompt()
    {
        var buffer = new StringBuilder();
        var cursorIndex = 0;
        StartEditing();

        while (true)
        {
            var key = ConsoleInput.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                StopEditing();
                return buffer.ToString();
            }

            if (key.Key == ConsoleKey.D && key.Modifiers == ConsoleModifiers.Control)
            {
                StopEditing();
                return null;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (cursorIndex > 0)
                {
                    buffer.Remove(cursorIndex - 1, 1);
                    cursorIndex--;
                }

                UpdateState(buffer.ToString(), cursorIndex);
                continue;
            }

            if (key.Key == ConsoleKey.Delete)
            {
                if (cursorIndex < buffer.Length)
                {
                    buffer.Remove(cursorIndex, 1);
                }

                UpdateState(buffer.ToString(), cursorIndex);
                continue;
            }

            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (cursorIndex > 0)
                {
                    cursorIndex--;
                }

                UpdateState(buffer.ToString(), cursorIndex);
                continue;
            }

            if (key.Key == ConsoleKey.RightArrow)
            {
                if (cursorIndex < buffer.Length)
                {
                    cursorIndex++;
                }

                UpdateState(buffer.ToString(), cursorIndex);
                continue;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                buffer.Clear();
                cursorIndex = 0;
                UpdateState(string.Empty, 0);
                continue;
            }

            if (key.KeyChar != '\0')
            {
                buffer.Insert(cursorIndex, key.KeyChar);
                cursorIndex++;
                UpdateState(buffer.ToString(), cursorIndex);
            }
        }
    }
}
