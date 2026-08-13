using System.IO;
using System.IO.Pipelines;
using System.Text;

namespace flconsole.Commands;

internal static class CommandTextStream
{
    public static Stream Create(string text)
    {
        return Create(async buffer =>
        {
            if (!string.IsNullOrEmpty(text))
            {
                await buffer.WriteAsync(text);
            }
        });
    }

    public static Stream Create(Func<CommandStreamBuffer, Task> producer)
    {
        var buffer = new CommandStreamBuffer();

        _ = Task.Run(async () =>
        {
            try
            {
                await producer(buffer);
            }
            catch (Exception ex)
            {
                await buffer.WriteAsync($"Error: {ex.Message}");
            }
            finally
            {
                await buffer.CompleteAsync();
            }
        });

        return buffer.AsStream();
    }
}

internal sealed class CommandStreamBuffer
{
    private readonly Pipe _pipe = new();

    public Stream AsStream()
    {
        return _pipe.Reader.AsStream();
    }

    public async Task WriteAsync(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await _pipe.Writer.WriteAsync(bytes);
    }

    public Task WriteLineAsync(string line)
    {
        return WriteAsync(line + Environment.NewLine);
    }

    public async Task CompleteAsync()
    {
        await _pipe.Writer.CompleteAsync();
    }
}