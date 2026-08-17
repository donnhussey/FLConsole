namespace flconsole.Commands;

public interface ICommand
{
    string CommandName { get; }
    bool Repeat { get; }
    TimeSpan RepeatInterval { get; }
    bool StopsShell { get; }
    Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default);
}

public interface ITxCommand : ICommand
{
}

public interface ITxIdentityRequiredCommand : ITxCommand
{
}
