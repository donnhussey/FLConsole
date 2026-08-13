using System.IO;

namespace flconsole.Commands;

public sealed class ClearCommand(ConsoleOutputBuffer outputBuffer, IRenderer renderer, IPromptState promptState) : ICommand<IReadOnlyList<string>>
{
    public string CommandName => "clear";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        outputBuffer.Clear();
        renderer.Clear();
        renderer.RenderOutput(outputBuffer);
        renderer.RenderInput(promptState.CurrentText, promptState.CurrentCursorIndex);
        return Task.FromResult<Stream>(CommandTextStream.Create(string.Empty));
    }
}