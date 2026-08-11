namespace flconsole;

public sealed class SystemConsoleFacade : IConsoleFacade
{
    public int WindowHeight => Console.WindowHeight;
    public int WindowWidth => Console.WindowWidth;

    public void Clear()
    {
        Console.Clear();
    }

    public void SetCursorPosition(int left, int top)
    {
        Console.SetCursorPosition(left, top);
    }

    public void Write(string text)
    {
        Console.Write(text);
    }
}
