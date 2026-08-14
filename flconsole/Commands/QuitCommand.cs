namespace flconsole.Commands;

public sealed class QuitCommand : ICommand
{
    public string CommandName => "quit";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => true;

    public Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
