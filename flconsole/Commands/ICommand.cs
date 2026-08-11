using System.IO;

namespace flconsole.Commands;

public interface ICommand<TRequest>
{
    string CommandName { get; }
    bool Repeat { get; }
    TimeSpan RepeatInterval { get; }
    bool StopsShell { get; }
    Task<Stream> ExecuteAsync(TRequest request);
}
