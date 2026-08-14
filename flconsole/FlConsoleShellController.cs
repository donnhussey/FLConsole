
using flconsole.Console;

namespace flconsole;

internal class FlConsoleShellController(ICommandResolver CommandResolver, CommandExecutor DisplayRunner, CommandMessages Messages)
{
    public virtual bool IsRunning { get; protected set; } = true;

    public virtual async Task HandleCommandAsync(ConsoleCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return;
        }

            await DispatchCommandAsync(request, cancellationToken);
    }

    public virtual async Task StopDisplayLoopAsync()
    {
        await DisplayRunner.StopAsync();
    }

    private async Task DispatchCommandAsync(ConsoleCommand request, CancellationToken cancellationToken)
    {
        var command = CommandResolver.Resolve(request.Name);
        if (command is null)
        {
            await DisplayRunner.WriteLineAsync(string.Format(Messages.UnknownCommandFormat, request.Name), cancellationToken);
            return;
        }

        if (command.StopsShell)
        {
            await DisplayRunner.RunToCompletionAsync(command, request.Arguments, cancellationToken);
            IsRunning = false;
            return;
        }

        await DisplayRunner.StartAsync(command, request.Arguments, cancellationToken);
    }
}
