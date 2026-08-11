namespace flconsole;

public interface IRenderer
{
    void RenderInput(string promptText, int cursorIndex);
    void RenderOutput(ConsoleOutputBuffer outputBuffer);
    void Clear();
}