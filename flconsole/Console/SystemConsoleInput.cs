namespace flconsole;

public sealed class SystemConsoleInput : IConsoleInput
{
    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        return Console.ReadKey(intercept);
    }
}
