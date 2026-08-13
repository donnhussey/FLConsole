using flconsole.Commands;

namespace flconsole;

public sealed class FlConsoleShellController(ICommandResolver<IReadOnlyList<string>> CommandResolver, CommandDisplayRunner DisplayRunner) : IShellController
{
    public bool IsRunning { get; private set; } = true;

    public async Task HandleInputAsync(string line)
    {
        var request = ShellCommandParser.Parse(line);
        if (request is null)
        {
            return;
        }

        await DispatchCommandAsync(request);
    }

    public async Task StopDisplayLoopAsync()
    {
        await DisplayRunner.StopAsync();
    }

    private async Task DispatchCommandAsync(ShellCommandRequest request)
    {
        var command = CommandResolver.Resolve(request.Name);
        if (command is null)
        {
            DisplayRunner.AppendLineAndRender($"Unknown command: {request.Name}. Type 'help' for commands.");
            return;
        }

        if (command.StopsShell)
        {
            await DisplayRunner.RunToCompletionAsync(command, request.Arguments);
            IsRunning = false;
            return;
        }

        await DisplayRunner.StartAsync(command, request.Arguments);
    }
}
