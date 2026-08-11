using System.IO;

namespace flconsole.Commands;

public sealed class QuitCommand : ICommand<IReadOnlyList<string>>
{
    public string CommandName => "quit";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => true;

    public Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        return Task.FromResult<Stream>(CommandTextStream.Create(string.Empty));
    }
}
