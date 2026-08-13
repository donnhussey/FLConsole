using System.Text;

namespace flconsole.Tests;

public class FlConsoleShellControllerTests
{
    private static readonly ShellMessages TestMessages = new("Unknown command: {0}. Type 'help' for commands.", "Error: {0}");
    [Fact]
    public async Task HandleInputAsync_BlankInput_IsIgnored()
    {
        var knownCommand = new TestCommand("help", responseText: "ok", repeat: false, repeatInterval: TimeSpan.Zero);
        var controller = CreateController([knownCommand], out var outputBuffer, out _);

        await controller.HandleCommandAsync(new ConsoleCommand("", []));

        Assert.Empty(outputBuffer.GetVisibleLines(10));
        Assert.True(controller.IsRunning);
    }

    [Fact]
    public async Task HandleInputAsync_UnknownCommand_AppendsHelpfulMessage()
    {
        var controller = CreateController([], out var outputBuffer, out _);

        await controller.HandleCommandAsync(new ConsoleCommand("doesnotexist", ["now"]));

        var lines = outputBuffer.GetVisibleLines(10).ToList();
        Assert.Single(lines);
        Assert.Equal("Unknown command: doesnotexist. Type 'help' for commands.", lines[0]);
    }

    [Fact]
    public async Task HandleInputAsync_QuitCommand_StopsControllerAndDisplayLoop()
    {
        var quitCommand = new TestCommand("quit", responseText: string.Empty, repeat: false, repeatInterval: TimeSpan.Zero);
        var controller = CreateController([quitCommand], out var outputBuffer, out _);

        await controller.HandleCommandAsync(new ConsoleCommand("quit", []));

        Assert.False(controller.IsRunning);
        Assert.Equal([string.Empty, string.Empty], outputBuffer.GetVisibleLines(10));
    }

    [Fact]
    public async Task HandleInputAsync_KnownCommand_ExecutesAndWritesOutput()
    {
        var command = new TestCommand("help", responseText: "hello from command", repeat: false, repeatInterval: TimeSpan.Zero);
        var controller = CreateController([command], out var outputBuffer, out _);

        await controller.HandleCommandAsync(new ConsoleCommand("help", ["one", "two"]));
        await WaitUntilAsync(() => command.LastRequest is not null);
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Any());
        await controller.StopDisplayLoopAsync();

        Assert.Equal(["one", "two"], command.LastRequest);
        var lines = outputBuffer.GetVisibleLines(10).ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal("hello from command", lines[0]);
        Assert.Equal(string.Empty, lines[1]);
    }

    [Fact]
    public async Task HandleInputAsync_ClearCommand_ClearsExistingOutput()
    {
        var outputBuffer = new ConsoleOutputBuffer(MaxLines: 20);
        var renderer = new FakePromptAreaRenderer(outputBuffer);
        var promptHandler = new ConsolePromptHandler(renderer, new EnterOnlyConsoleInput());
        var runner = new CommandDisplayRunner(renderer, promptHandler, TestMessages);
        var resolver = new CommandResolver<IReadOnlyList<string>>([
            new ClearCommand(renderer, promptHandler)
        ]);
        var controller = new FlConsoleShellController(resolver, runner, TestMessages);

        outputBuffer.AddLine("existing output");

        await controller.HandleCommandAsync(new ConsoleCommand("clear", []));
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Count == 2);
        await controller.StopDisplayLoopAsync();

        Assert.Equal([string.Empty, string.Empty], outputBuffer.GetVisibleLines(10));
    }

    [Fact]
    public async Task HandleInputAsync_CommandThrows_AppendsErrorLine()
    {
        var command = new TestCommand("help", responseText: "ignored", repeat: false, repeatInterval: TimeSpan.Zero)
        {
            ThrowOnExecute = true,
            ExceptionMessage = "boom"
        };
        var controller = CreateController([command], out var outputBuffer, out _);

        await controller.HandleCommandAsync(new ConsoleCommand("help", []));
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Any());
        await controller.StopDisplayLoopAsync();

        var lines = outputBuffer.GetVisibleLines(10).ToList();
        Assert.Single(lines);
        Assert.Equal("Error: boom", lines[0]);
    }

    private static FlConsoleShellController CreateController(
        IEnumerable<ICommand<IReadOnlyList<string>>> commands,
        out ConsoleOutputBuffer outputBuffer,
        out FakePromptAreaRenderer renderer)
    {
        outputBuffer = new ConsoleOutputBuffer(MaxLines: 20);
        renderer = new FakePromptAreaRenderer(outputBuffer);
        var promptHandler = new ConsolePromptHandler(renderer, new EnterOnlyConsoleInput());
        var runner = new CommandDisplayRunner(renderer, promptHandler, TestMessages);
        var resolver = new CommandResolver<IReadOnlyList<string>>(commands);

        return new FlConsoleShellController(resolver, runner, TestMessages);
    }

    private sealed class TestCommand(string commandName, string responseText, bool repeat, TimeSpan repeatInterval)
        : ICommand<IReadOnlyList<string>>
    {
        public string CommandName { get; } = commandName;
        public bool Repeat { get; } = repeat;
        public TimeSpan RepeatInterval { get; } = repeatInterval;
        public bool StopsShell => string.Equals(CommandName, "quit", StringComparison.OrdinalIgnoreCase);
        public IReadOnlyList<string>? LastRequest { get; private set; }
        public bool ThrowOnExecute { get; set; }
        public string ExceptionMessage { get; set; } = "error";

        public Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
        {
            LastRequest = request.ToList();
            if (ThrowOnExecute)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }

            return Task.FromResult<Stream>(
                new MemoryStream(Encoding.UTF8.GetBytes(responseText)));
        }
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(2);
        while (!predicate())
        {
            if (DateTime.UtcNow >= timeoutAt)
            {
                throw new TimeoutException("Condition was not met in time.");
            }

            await Task.Delay(10);
        }
    }
}