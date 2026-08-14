using System.Text;

namespace flconsole.Tests;

public class FlConsoleApplicationTests
{
    [Fact]
    public async Task RunAsync_HelpFlag_WritesHelpAndSkipsInteractiveLoop()
    {
        var renderer = new AppTestRenderer();
        var promptReader = new AppTestPromptReader([new ConsoleCommand("help", [])]);
        var console = new global::flconsole.Console.Console(promptReader, renderer);
        var shellController = new AppTestShellController();
        var app = new FlConsoleApplication(
            console,
            new XmlRpcConnectionSettings("127.0.0.1", 7362),
            CommandMessages.Defaults,
            shellController);
        var output = new StringWriter();

        var exitCode = await app.RunAsync(["--help"], output);

        Assert.Equal(0, exitCode);
        Assert.Equal(CommandMessages.Defaults.HelpText + Environment.NewLine, output.ToString());
        Assert.Equal(0, renderer.ClearCallCount);
        Assert.Empty(shellController.HandledInputs);
        Assert.Equal(0, shellController.StopCallCount);
    }

    [Fact]
    public async Task RunAsync_InteractiveMode_RendersBannerAndProcessesInputUntilNull()
    {
        var renderer = new AppTestRenderer();
        var promptReader = new AppTestPromptReader([new ConsoleCommand("help", []), new ConsoleCommand("monitor", []), null]);
        var console = new global::flconsole.Console.Console(promptReader, renderer);
        var shellController = new AppTestShellController();
        var app = new FlConsoleApplication(
            console,
            new XmlRpcConnectionSettings("10.0.0.5", 9999),
            CommandMessages.Defaults,
            shellController);

        var exitCode = await app.RunAsync([], new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.Equal(1, renderer.ClearCallCount);
        Assert.True(renderer.Lines.Count >= 2);
        Assert.Equal(1, renderer.ShowPromptCallCount);
        Assert.Equal(["help", "monitor"], shellController.HandledInputs);
        Assert.Equal(1, shellController.StopCallCount);

        Assert.Equal("FLDigi XML-RPC shell (host=10.0.0.5, port=9999)", renderer.Lines[0]);
        Assert.Equal("Type 'help' for commands, or 'quit' to exit.", renderer.Lines[1]);
    }

    [Fact]
    public async Task RunAsync_InteractiveMode_StopsWhenControllerNotRunning()
    {
        var renderer = new AppTestRenderer();
        var promptReader = new AppTestPromptReader([new ConsoleCommand("help", [])]);
        var console = new global::flconsole.Console.Console(promptReader, renderer);
        var shellController = new AppTestShellController { IsRunning = false };
        var app = new FlConsoleApplication(
            console,
            new XmlRpcConnectionSettings("127.0.0.1", 7362),
            CommandMessages.Defaults,
            shellController);

        var exitCode = await app.RunAsync([], new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.Empty(shellController.HandledInputs);
        Assert.Equal(1, shellController.StopCallCount);
    }

    private sealed class AppTestRenderer : IConsoleDisplay
    {
        public List<string> Lines { get; } = [];
        public int ClearCallCount { get; private set; }
        public int AppendTextCallCount { get; private set; }
        public int ShowPromptCallCount { get; private set; }

        public void Clear()
        {
            ClearCallCount++;
        }

        public void ShowPrompt(string promptText, int cursorIndex)
        {
            ShowPromptCallCount++;
        }

        public void AppendText(string text)
        {
            AppendTextCallCount++;
        }

        public void AppendLine(string text)
        {
            Lines.Add(text);
        }
    }

    private sealed class AppTestPromptReader(IEnumerable<ConsoleCommand?> commands) : ICommandSource
    {
        private readonly Queue<ConsoleCommand?> _commands = new(commands);

        public ConsolePromptState PromptState => new(string.Empty, 0);

        public ConsoleCommand? ReadCommand()
        {
            return _commands.Count > 0 ? _commands.Dequeue() : null;
        }
    }

    private sealed class AppTestShellController : FlConsoleShellController
    {
        public AppTestShellController() : base(null!, null!, CommandMessages.Defaults)
        {
        }

        public new bool IsRunning
        {
            get => base.IsRunning;
            set => base.IsRunning = value;
        }

        public List<string> HandledInputs { get; } = [];
        public int StopCallCount { get; private set; }

        public override Task HandleCommandAsync(ConsoleCommand request, CancellationToken cancellationToken = default)
        {
            HandledInputs.Add(request.Name);
            return Task.CompletedTask;
        }

        public override Task StopDisplayLoopAsync()
        {
            StopCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class AppTestCommand(string commandName, string responseText) : ICommand
    {
        public string CommandName { get; } = commandName;
        public bool Repeat => false;
        public TimeSpan RepeatInterval => TimeSpan.Zero;
        public bool StopsShell => false;

        public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
        {
            await output.WriteAsync(responseText, cancellationToken);
        }
    }
}