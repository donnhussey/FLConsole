namespace flconsole.Commands;

public sealed class ClearCommand(IConsoleDisplay display, ICommandSource commandSource) : ICommand
{
    public string CommandName => "clear";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        display.Clear();
        var prompt = commandSource.PromptState;
        display.ShowPrompt(prompt.Text, prompt.CursorIndex);
        return Task.CompletedTask;
    }
}