using System.Text;

namespace flconsole.Tests;

public class FlConsoleShellControllerTests
{
    [Fact]
    public async Task HandleInputAsync_BlankInput_IsIgnored()
    {
        var knownCommand = new TestCommand("help", responseText: "ok", repeat: false, repeatInterval: TimeSpan.Zero);
        var controller = CreateController([knownCommand], out var outputBuffer, out _);

        await controller.HandleInputAsync("   ");

        Assert.Empty(outputBuffer.GetVisibleLines(10));
        Assert.True(controller.IsRunning);
    }

    [Fact]
    public async Task HandleInputAsync_UnknownCommand_AppendsHelpfulMessage()
    {
        var controller = CreateController([], out var outputBuffer, out _);

        await controller.HandleInputAsync("doesnotexist now");

        var lines = outputBuffer.GetVisibleLines(10).ToList();
        Assert.Single(lines);
        Assert.Equal("Unknown command: doesnotexist. Type 'help' for commands.", lines[0]);
    }

    [Fact]
    public async Task HandleInputAsync_QuitCommand_StopsControllerAndDisplayLoop()
    {
        var quitCommand = new TestCommand("quit", responseText: string.Empty, repeat: false, repeatInterval: TimeSpan.Zero);
        var controller = CreateController([quitCommand], out _, out _);

        await controller.HandleInputAsync("quit");

        Assert.False(controller.IsRunning);
    }

    [Fact]
    public async Task HandleInputAsync_KnownCommand_ExecutesAndWritesOutput()
    {
        var command = new TestCommand("help", responseText: "hello from command", repeat: false, repeatInterval: TimeSpan.Zero);
        var controller = CreateController([command], out var outputBuffer, out _);

        await controller.HandleInputAsync("help one two");
        await WaitUntilAsync(() => command.LastRequest is not null);
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Any());
        await controller.StopDisplayLoopAsync();

        Assert.Equal(["one", "two"], command.LastRequest);
        var lines = outputBuffer.GetVisibleLines(10).ToList();
        Assert.Single(lines);
        Assert.Equal("hello from command", lines[0]);
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

        await controller.HandleInputAsync("help");
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
        renderer = new FakePromptAreaRenderer();
        outputBuffer = new ConsoleOutputBuffer(MaxLines: 20);
        var promptHandler = new ConsolePromptHandler(renderer, new EnterOnlyConsoleInput());
        var runner = new CommandDisplayRunner(renderer, outputBuffer, promptHandler);
        var resolver = new CommandResolver<IReadOnlyList<string>>(commands);

        return new FlConsoleShellController(resolver, runner);
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