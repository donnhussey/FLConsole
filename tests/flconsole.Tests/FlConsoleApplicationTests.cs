using System.Text;

namespace flconsole.Tests;

public class FlConsoleApplicationTests
{
    [Fact]
    public async Task RunAsync_HelpFlag_WritesHelpAndSkipsInteractiveLoop()
    {
        var helpCommand = new AppTestCommand("help", "help text");
        var resolver = new CommandResolver<IReadOnlyList<string>>([helpCommand]);
        var renderer = new AppTestRenderer();
        var outputBuffer = new ConsoleOutputBuffer(MaxLines: 20);
        var promptReader = new AppTestPromptReader(["should-not-be-read"]);
        var shellController = new AppTestShellController();
        var app = new FlConsoleApplication(
            resolver,
            renderer,
            outputBuffer,
            promptReader,
            new XmlRpcConnectionSettings("127.0.0.1", 7362),
            shellController);
        var output = new StringWriter();

        var exitCode = await app.RunAsync(["--help"], output);

        Assert.Equal(0, exitCode);
        Assert.Equal("help text" + Environment.NewLine, output.ToString());
        Assert.Equal(0, renderer.ClearCallCount);
        Assert.Empty(shellController.HandledInputs);
        Assert.Equal(0, shellController.StopCallCount);
    }

    [Fact]
    public async Task RunAsync_InteractiveMode_RendersBannerAndProcessesInputUntilNull()
    {
        var resolver = new CommandResolver<IReadOnlyList<string>>([new AppTestCommand("help", "ignored")]);
        var renderer = new AppTestRenderer();
        var outputBuffer = new ConsoleOutputBuffer(MaxLines: 20);
        var promptReader = new AppTestPromptReader(["help", "monitor", null]);
        var shellController = new AppTestShellController();
        var app = new FlConsoleApplication(
            resolver,
            renderer,
            outputBuffer,
            promptReader,
            new XmlRpcConnectionSettings("10.0.0.5", 9999),
            shellController);

        var exitCode = await app.RunAsync([], new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.Equal(1, renderer.ClearCallCount);
        Assert.Equal(1, renderer.RenderOutputCallCount);
        Assert.Equal(1, renderer.RenderInputCallCount);
        Assert.Equal(["help", "monitor"], shellController.HandledInputs);
        Assert.Equal(1, shellController.StopCallCount);

        var lines = outputBuffer.GetVisibleLines(10).ToList();
        Assert.Equal("FLDigi XML-RPC shell (host=10.0.0.5, port=9999)", lines[0]);
        Assert.Equal("Type 'help' for commands, or 'quit' to exit.", lines[1]);
    }

    [Fact]
    public async Task RunAsync_InteractiveMode_StopsWhenControllerNotRunning()
    {
        var resolver = new CommandResolver<IReadOnlyList<string>>([new AppTestCommand("help", "ignored")]);
        var renderer = new AppTestRenderer();
        var outputBuffer = new ConsoleOutputBuffer(MaxLines: 20);
        var promptReader = new AppTestPromptReader(["help"]);
        var shellController = new AppTestShellController { IsRunning = false };
        var app = new FlConsoleApplication(
            resolver,
            renderer,
            outputBuffer,
            promptReader,
            new XmlRpcConnectionSettings("127.0.0.1", 7362),
            shellController);

        var exitCode = await app.RunAsync([], new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.Empty(shellController.HandledInputs);
        Assert.Equal(1, shellController.StopCallCount);
    }

    private sealed class AppTestRenderer : IRenderer
    {
        public int ClearCallCount { get; private set; }
        public int RenderOutputCallCount { get; private set; }
        public int RenderInputCallCount { get; private set; }

        public void Clear()
        {
            ClearCallCount++;
        }

        public void RenderOutput(ConsoleOutputBuffer outputBuffer)
        {
            RenderOutputCallCount++;
        }

        public void RenderInput(string promptText, int cursorIndex)
        {
            RenderInputCallCount++;
        }
    }

    private sealed class AppTestPromptReader(IEnumerable<string?> lines) : IPromptReader
    {
        private readonly Queue<string?> _lines = new(lines);

        public string? ReadLineFromPrompt()
        {
            return _lines.Count > 0 ? _lines.Dequeue() : null;
        }
    }

    private sealed class AppTestShellController : IShellController
    {
        public bool IsRunning { get; set; } = true;
        public List<string> HandledInputs { get; } = [];
        public int StopCallCount { get; private set; }

        public Task HandleInputAsync(string line)
        {
            HandledInputs.Add(line);
            return Task.CompletedTask;
        }

        public Task StopDisplayLoopAsync()
        {
            StopCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class AppTestCommand(string commandName, string responseText) : ICommand<IReadOnlyList<string>>
    {
        public string CommandName { get; } = commandName;
        public bool Repeat => false;
        public TimeSpan RepeatInterval => TimeSpan.Zero;
        public bool StopsShell => false;

        public Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
        {
            return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(responseText)));
        }
    }
}