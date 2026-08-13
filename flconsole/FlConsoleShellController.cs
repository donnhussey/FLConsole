
using flconsole.Console;

namespace flconsole;

internal sealed class FlConsoleShellController(ICommandResolver<IReadOnlyList<string>> CommandResolver, CommandDisplayRunner DisplayRunner, ShellMessages Messages) : IShellController
{
    public bool IsRunning { get; private set; } = true;

    public async Task HandleCommandAsync(ConsoleCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return;
        }

        await DispatchCommandAsync(request);
    }

    public async Task StopDisplayLoopAsync()
    {
        await DisplayRunner.StopAsync();
    }

    private async Task DispatchCommandAsync(ConsoleCommand request)
    {
        var command = CommandResolver.Resolve(request.Name);
        if (command is null)
        {
            DisplayRunner.AppendLineAndRender(string.Format(Messages.UnknownCommandFormat, request.Name));
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
