namespace flconsole.Console;

public interface IConsole
{
    ICommandSource CommandSource { get; }
    IConsoleDisplay Display { get; }
}
