using flconsole.Commands;

namespace flconsole;

internal sealed class FlConsoleApplication(
    IConsole console,
    XmlRpcConnectionSettings connectionSettings,
    CommandMessages commandMessages,
    FlConsoleShellController shellController)
{
    public async Task<int> RunAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            await output.WriteLineAsync(commandMessages.HelpText);
            return 0;
        }

        console.Display.Clear();
        console.Display.AppendLine($"FLDigi XML-RPC shell (host={connectionSettings.Host}, port={connectionSettings.Port})");
        console.Display.AppendLine(commandMessages.StartupHint);
        console.Display.ShowPrompt(string.Empty, 0);

        while (shellController.IsRunning && !cancellationToken.IsCancellationRequested)
        {
            var command = console.CommandSource.ReadCommand();
            if (command is null)
            {
                break;
            }

            await shellController.HandleCommandAsync(command, cancellationToken);
        }

        await shellController.StopDisplayLoopAsync();
        return 0;
    }

}