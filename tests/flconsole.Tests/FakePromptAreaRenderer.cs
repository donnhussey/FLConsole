namespace flconsole.Tests;

internal sealed class FakePromptAreaRenderer : IRenderer
{
    public string? LastPromptText { get; private set; }
    public int LastCursorIndex { get; private set; } = -1;

    public void Clear()
    {
    }

    public void RenderOutput(ConsoleOutputBuffer outputBuffer)
    {
    }

    public void RenderInput(string promptText, int cursorIndex)
    {
        LastPromptText = promptText;
        LastCursorIndex = cursorIndex;
    }
}
