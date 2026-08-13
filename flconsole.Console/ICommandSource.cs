namespace flconsole.Console;

public interface ICommandSource
{
    ConsolePromptState PromptState { get; }
    ConsoleCommand? ReadCommand();
}