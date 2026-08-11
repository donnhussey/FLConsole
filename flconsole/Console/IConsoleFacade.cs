namespace flconsole;

public interface IConsoleFacade
{
    int WindowHeight { get; }
    int WindowWidth { get; }
    void Clear();
    void SetCursorPosition(int left, int top);
    void Write(string text);
}