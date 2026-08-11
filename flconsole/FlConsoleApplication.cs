using flconsole.Commands;

namespace flconsole;

public sealed class FlConsoleApplication(
    ICommandResolver<IReadOnlyList<string>> commandResolver,
    IRenderer renderer,
    ConsoleOutputBuffer outputBuffer,
    IPromptReader promptReader,
    XmlRpcConnectionSettings connectionSettings,
    IShellController shellController)
{
    public async Task<int> RunAsync(string[] args, TextWriter output)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            return await PrintUsageAsync(output);
        }

        renderer.Clear();
        outputBuffer.AddLine($"FLDigi XML-RPC shell (host={connectionSettings.Host}, port={connectionSettings.Port})");
        outputBuffer.AddLine("Type 'help' for commands, or 'quit' to exit.");
        renderer.RenderOutput(outputBuffer);
        renderer.RenderInput(string.Empty, 0);

        while (shellController.IsRunning)
        {
            var line = promptReader.ReadLineFromPrompt();
            if (line is null)
            {
                break;
            }

            await shellController.HandleInputAsync(line);
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