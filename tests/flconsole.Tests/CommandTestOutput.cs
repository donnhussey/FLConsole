using System.Text;
using System.IO;

namespace flconsole.Tests;

internal sealed class CommandTestOutput : ICommandOutput
{
    private readonly StringBuilder _text = new();

    public ValueTask WriteAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _text.Append(text);
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteLineAsync(string text, CancellationToken cancellationToken = default)
    {
        return WriteAsync(text + Environment.NewLine, cancellationToken);
    }

    public override string ToString() => _text.ToString();
}

internal static class CommandTestExtensions
{
    public static async Task<Stream> ExecuteAsync(this ICommand command, IReadOnlyList<string> arguments)
    {
        var output = await command.ExecuteAndReadAsync(arguments);
        return new MemoryStream(Encoding.UTF8.GetBytes(output));
    }

    public static async Task<string> ExecuteAndReadAsync(this ICommand command, IReadOnlyList<string> arguments)
    {
        var output = new CommandTestOutput();
        await command.ExecuteAsync(arguments, output);
        return output.ToString();
    }
}
