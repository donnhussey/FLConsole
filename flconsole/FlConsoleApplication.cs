using flconsole.Commands;

namespace flconsole;

public sealed class FlConsoleApplication(
    ICommandResolver<IReadOnlyList<string>> commandResolver,
    IConsole console,
    XmlRpcConnectionSettings connectionSettings,
    IShellController shellController)
{
    public async Task<int> RunAsync(string[] args, TextWriter output)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            return await PrintUsageAsync(output);
        }

        console.Display.Clear();
        console.Display.AppendLine($"FLDigi XML-RPC shell (host={connectionSettings.Host}, port={connectionSettings.Port})");
        console.Display.AppendLine("Type 'help' for commands, or 'quit' to exit.");
        console.Display.ShowPrompt(string.Empty, 0);

        while (shellController.IsRunning)
        {
            var command = console.CommandSource.ReadCommand();
            if (command is null)
            {
                break;
            }

            await shellController.HandleCommandAsync(command);
        }

        await shellController.StopDisplayLoopAsync();
        return 0;
    }

    private async Task<int> PrintUsageAsync(TextWriter output)
    {
        var helpCommand = commandResolver.Resolve("help")
            ?? throw new InvalidOperationException("Help command is not registered.");
        using var stream = await helpCommand.ExecuteAsync(Array.Empty<string>());
        using var reader = new StreamReader(stream);
        await output.WriteLineAsync(await reader.ReadToEndAsync());
        return 0;
    }
}