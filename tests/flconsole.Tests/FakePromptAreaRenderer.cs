namespace flconsole.Tests;

internal sealed class FakePromptAreaRenderer : IConsoleDisplay
{
    public FakePromptAreaRenderer(ConsoleOutputBuffer? outputBuffer = null)
    {
        OutputBuffer = outputBuffer ?? new ConsoleOutputBuffer();
    }

    public ConsoleOutputBuffer OutputBuffer { get; }
    public string? LastPromptText { get; private set; }
    public int LastCursorIndex { get; private set; } = -1;

    public void Clear()
    {
        OutputBuffer.Clear();
    }

    public void ShowPrompt(string promptText, int cursorIndex)
    {
        LastPromptText = promptText;
        LastCursorIndex = cursorIndex;
    }

    public void AppendText(string text)
    {
        OutputBuffer.AppendText(text);
    }

    public void AppendLine(string text)
    {
        OutputBuffer.AddLine(text);
    }
}
