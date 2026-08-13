namespace flconsole.Console;

internal interface IConsoleInput
{
    ConsoleKeyInfo ReadKey(bool intercept);
}

internal interface IConsoleTerminal
{
    int WindowHeight { get; }
    int WindowWidth { get; }
    void Clear();
    void SetCursorPosition(int left, int top);
    void Write(string text);
}

internal sealed class SystemConsoleInput : IConsoleInput
{
    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        return global::System.Console.ReadKey(intercept);
    }
}

internal sealed class SystemConsoleTerminal : IConsoleTerminal
{
    public int WindowHeight => global::System.Console.WindowHeight;
    public int WindowWidth => global::System.Console.WindowWidth;

    public void Clear()
    {
        global::System.Console.Clear();
    }

    public void SetCursorPosition(int left, int top)
    {
        global::System.Console.SetCursorPosition(left, top);
    }

    public void Write(string text)
    {
        global::System.Console.Write(text);
    }
}
