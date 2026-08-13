namespace flconsole.Console;

internal sealed class Console : IConsole
{
    internal Console(ICommandSource commandSource, IConsoleDisplay display)
    {
        CommandSource = commandSource ?? throw new ArgumentNullException(nameof(commandSource));
        Display = display ?? throw new ArgumentNullException(nameof(display));
    }

    public ICommandSource CommandSource { get; }
    public IConsoleDisplay Display { get; }
}