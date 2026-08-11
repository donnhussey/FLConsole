namespace flconsole.Tests;

internal sealed class QueueConsoleInput(IEnumerable<ConsoleKeyInfo> keys) : IConsoleInput
{
    private readonly Queue<ConsoleKeyInfo> _keys = new(keys);

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        if (_keys.Count == 0)
        {
            throw new InvalidOperationException("No queued key input available.");
        }

        return _keys.Dequeue();
    }
}