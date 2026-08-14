namespace flconsole.Commands;

public interface ICommandOutput
{
    ValueTask WriteAsync(string text, CancellationToken cancellationToken = default);
    ValueTask WriteLineAsync(string text, CancellationToken cancellationToken = default);
}
