namespace flconsole.Console;

internal sealed class ConsolePromptHandler(IConsoleDisplay display, IConsoleInput consoleInput) : ICommandSource
{
    private const int MaximumHistoryEntries = 30;
    private readonly PromptEditor _editor = new();
    private readonly List<string> _history = [];
    private ConsolePromptState _promptState = new(string.Empty, 0);
    private int _historyIndex;
    private string _draft = string.Empty;

    public ConsolePromptState PromptState => _promptState;

    public ConsoleCommand? ReadCommand()
    {
        _editor.Reset();
        _historyIndex = _history.Count;
        _draft = string.Empty;
        UpdatePromptState();
        ShowPrompt();

        while (true)
        {
            var key = consoleInput.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow)
            {
                MoveHistory(-1);
                UpdatePromptState();
                ShowPrompt();
                continue;
            }

            if (key.Key == ConsoleKey.DownArrow)
            {
                MoveHistory(1);
                UpdatePromptState();
                ShowPrompt();
                continue;
            }

            var result = _editor.Apply(key);
            UpdatePromptState();
            if (result == PromptEditResult.Submit)
            {
                Remember(_editor.Text);
                ShowPrompt();
                return ParseCommand(_editor.Text);
            }

            if (result == PromptEditResult.EndOfInput)
            {
                ShowPrompt();
                return null;
            }

            ShowPrompt();
        }
    }

    private void ShowPrompt()
    {
        display.ShowPrompt(_promptState.Text, _promptState.CursorIndex);
    }

    private void UpdatePromptState()
    {
        _promptState = new ConsolePromptState(_editor.Text, _editor.CursorIndex);
    }

    private void MoveHistory(int direction)
    {
        if (_history.Count == 0)
        {
            return;
        }

        if (_historyIndex == _history.Count && direction < 0)
        {
            _draft = _editor.Text;
        }

        var nextIndex = Math.Clamp(_historyIndex + direction, 0, _history.Count);
        if (nextIndex == _historyIndex)
        {
            return;
        }

        _historyIndex = nextIndex;
        _editor.SetText(_historyIndex == _history.Count ? _draft : _history[_historyIndex]);
    }

    private void Remember(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var command = text.Trim();
        if (_history.Count > 0 && string.Equals(_history[^1], command, StringComparison.Ordinal))
        {
            return;
        }

        _history.Add(command);
        if (_history.Count > MaximumHistoryEntries)
        {
            _history.RemoveAt(0);
        }
    }

    private static ConsoleCommand? ParseCommand(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var parts = line.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new ConsoleCommand(parts[0].ToLowerInvariant(), parts.Skip(1).ToList());
    }
}
