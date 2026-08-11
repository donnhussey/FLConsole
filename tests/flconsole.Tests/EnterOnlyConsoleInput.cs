namespace flconsole.Tests;

internal sealed class EnterOnlyConsoleInput : IConsoleInput
{
    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        return new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
    }
}
