namespace flconsole.Console;

internal sealed class ConsolePromptHandler(IConsoleDisplay display, IConsoleInput consoleInput) : ICommandSource
{
    private readonly PromptEditor _editor = new();
    private ConsolePromptState _promptState = new(string.Empty, 0);

    public ConsolePromptState PromptState => _promptState;

    public ConsoleCommand? ReadCommand()
    {
        _editor.Reset();
        UpdatePromptState();
        ShowPrompt();

        while (true)
        {
            var result = _editor.Apply(consoleInput.ReadKey(true));
            UpdatePromptState();
            if (result == PromptEditResult.Submit)
            {
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
