namespace flconsole.Console;

public sealed record ConsolePromptState(string Text, int CursorIndex);

internal enum PromptEditResult
{
    Continue,
    Submit,
    EndOfInput
}

internal sealed class PromptEditor
{
    private readonly StringBuilder _text = new();

    public int CursorIndex { get; private set; }
    public string Text => _text.ToString();

    public void Reset()
    {
        _text.Clear();
        CursorIndex = 0;
    }

    public void SetText(string text)
    {
        _text.Clear();
        _text.Append(text);
        CursorIndex = _text.Length;
    }

    public PromptEditResult Apply(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Enter)
        {
            return PromptEditResult.Submit;
        }

        if (key.Key == ConsoleKey.D && key.Modifiers == ConsoleModifiers.Control)
        {
            return PromptEditResult.EndOfInput;
        }

        switch (key.Key)
        {
            case ConsoleKey.Backspace:
                if (CursorIndex > 0)
                {
                    _text.Remove(CursorIndex - 1, 1);
                    CursorIndex--;
                }
                break;
            case ConsoleKey.Delete:
                if (CursorIndex < _text.Length)
                {
                    _text.Remove(CursorIndex, 1);
                }
                break;
            case ConsoleKey.LeftArrow:
                if (CursorIndex > 0)
                {
                    CursorIndex--;
                }
                break;
            case ConsoleKey.RightArrow:
                if (CursorIndex < _text.Length)
                {
                    CursorIndex++;
                }
                break;
            case ConsoleKey.Escape:
                Reset();
                break;
            default:
                if (key.KeyChar != '\0')
                {
                    _text.Insert(CursorIndex, key.KeyChar);
                    CursorIndex++;
                }
                break;
        }

        return PromptEditResult.Continue;
    }
}
