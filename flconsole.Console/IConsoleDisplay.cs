namespace flconsole.Console;

public interface IConsoleDisplay
{
    void ShowPrompt(string promptText, int cursorIndex);
    void Clear();
    void AppendText(string text);
    void AppendLine(string text);
}
