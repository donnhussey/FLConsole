namespace flconsole.Console;

public static class ConsoleFactory
{
    public static IConsole Create(string promptPrefix, int maxLines)
    {
        ArgumentNullException.ThrowIfNull(promptPrefix);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLines);
        var terminal = new SystemConsoleTerminal();
        var display = new ConsoleDisplay(promptPrefix, maxLines, terminal);
        var commandSource = new ConsolePromptHandler(display, new SystemConsoleInput());
        return new Console(commandSource, display);
    }
}