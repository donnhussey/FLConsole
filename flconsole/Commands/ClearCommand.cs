using System.IO;

namespace flconsole.Commands;

public sealed class ClearCommand(IConsoleDisplay display, ICommandSource commandSource) : ICommand<IReadOnlyList<string>>
{
    public string CommandName => "clear";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        display.Clear();
        var prompt = commandSource.PromptState;
        display.ShowPrompt(prompt.Text, prompt.CursorIndex);
        return Task.FromResult<Stream>(CommandTextStream.Create(string.Empty));
    }
}