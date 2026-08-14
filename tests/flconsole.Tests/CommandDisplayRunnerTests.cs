using System.Text;

namespace flconsole.Tests;

public class CommandExecutorTests
{
    private static readonly CommandMessages TestMessages = CommandMessages.Defaults with
    {
        UnknownCommandFormat = "Unknown command: {0}. Type 'help' for commands.",
        ExecutionErrorFormat = "Error: {0}"
    };
    [Fact]
    public async Task StopAsync_WhenNotStarted_ReturnsCleanly()
    {
        var runner = CreateRunner(out _, out _);

        await runner.StopAsync();
    }

    [Fact]
    public async Task StartAsync_NonRepeatingCommand_AppendsOutputAndRenders()
    {
        var runner = CreateRunner(out var renderer, out var outputBuffer);
        var command = new RunnerTestCommand("once", repeat: false, TimeSpan.Zero);

        await runner.StartAsync(command, Array.Empty<string>());
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Count == 2);
        await runner.StopAsync();

        Assert.Equal(1, command.ExecuteCount);
        Assert.Equal(["once", string.Empty], outputBuffer.GetVisibleLines(10));
        Assert.True(renderer.AppendTextCallCount >= 1);
        Assert.True(renderer.ShowPromptCallCount >= 1);
    }

    [Fact]
    public async Task StartAsync_NonRepeatingCommand_AppendsChunkedTextWithoutForcingNewLines()
    {
        var runner = CreateRunner(out _, out var outputBuffer);
        var command = new RunnerTestCommand("going region", repeat: false, TimeSpan.Zero);

        outputBuffer.AppendText("existing ");
        await runner.StartAsync(command, Array.Empty<string>());
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Count == 2);
        await runner.StopAsync();

        Assert.Equal(["existing going region", string.Empty], outputBuffer.GetVisibleLines(10));
    }

    [Fact]
    public async Task StartAsync_RendersStreamIncrementally()
    {
        var runner = CreateRunner(out _, out var outputBuffer);
        var command = new IncrementalRunnerTestCommand();

        await runner.StartAsync(command, Array.Empty<string>());
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Any(line => line.Contains("first", StringComparison.Ordinal)));

        var interimLines = outputBuffer.GetVisibleLines(10);
        Assert.Equal(["first"], interimLines);

        command.ReleaseSecondChunk();
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Count == 2);
        await runner.StopAsync();

        Assert.Equal(["first second", string.Empty], outputBuffer.GetVisibleLines(10));
    }

    [Fact]
    public async Task StartAsync_EmptyResponse_DoesNotAppendOutput()
    {
        var runner = CreateRunner(out _, out var outputBuffer);
        var command = new RunnerTestCommand(string.Empty, repeat: false, TimeSpan.Zero);

        await runner.StartAsync(command, Array.Empty<string>());
        await WaitUntilAsync(() => command.ExecuteCount >= 1);
        await runner.StopAsync();

        Assert.Equal([string.Empty, string.Empty], outputBuffer.GetVisibleLines(10));
    }

    [Fact]
    public async Task StartAsync_RepeatCommand_CanBeStoppedDuringDelay()
    {
        var runner = CreateRunner(out _, out _);
        var command = new RunnerTestCommand("tick", repeat: true, TimeSpan.FromSeconds(5));

        await runner.StartAsync(command, Array.Empty<string>());
        await WaitUntilAsync(() => command.ExecuteCount >= 1);
        await runner.StopAsync();

        var executedBeforeStop = command.ExecuteCount;
        Assert.True(executedBeforeStop >= 1);
    }

    [Fact]
    public async Task StartAsync_CommandThrows_AppendsErrorOutput()
    {
        var runner = CreateRunner(out _, out var outputBuffer);
        var command = new RunnerTestCommand("ignored", repeat: false, TimeSpan.Zero)
        {
            ThrowOnExecute = true,
            ExceptionMessage = "runner failed"
        };

        await runner.StartAsync(command, Array.Empty<string>());
        await WaitUntilAsync(() => outputBuffer.GetVisibleLines(10).Any());
        await runner.StopAsync();

        var lines = outputBuffer.GetVisibleLines(10).ToList();
        Assert.Single(lines);
        Assert.Equal("Error: runner failed", lines[0]);
    }

    [Fact]
    public async Task StartAsync_CancelledWhileExecuting_StopsWithoutOutput()
    {
        var runner = CreateRunner(out _, out var outputBuffer);
        var command = new CancelAwareRunnerTestCommand();

        await runner.StartAsync(command, Array.Empty<string>());
        await WaitUntilAsync(() => command.HasEnteredExecute);

        var stopTask = runner.StopAsync();
        command.ThrowOperationCanceled = true;
        command.ReleaseExecution();
        await stopTask;

        Assert.Empty(outputBuffer.GetVisibleLines(10));
    }

    private static CommandExecutor CreateRunner(out RunnerTestRenderer renderer, out ConsoleOutputBuffer outputBuffer)
    {
        renderer = new RunnerTestRenderer();
        outputBuffer = renderer.OutputBuffer;
        var promptHandler = new ConsolePromptHandler(renderer, new EnterOnlyConsoleInput());
        return new CommandExecutor(renderer, promptHandler, TestMessages);
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

    private sealed class RunnerTestRenderer : IConsoleDisplay
    {
        public ConsoleOutputBuffer OutputBuffer { get; } = new(MaxLines: 20);
        public int AppendTextCallCount { get; private set; }
        public int ShowPromptCallCount { get; private set; }

        public void ShowPrompt(string promptText, int cursorIndex)
        {
            ShowPromptCallCount++;
        }

        public void Clear()
        {
            OutputBuffer.Clear();
        }

        public void AppendText(string text)
        {
            AppendTextCallCount++;
            OutputBuffer.AppendText(text);
        }

        public void AppendLine(string text)
        {
            OutputBuffer.AddLine(text);
        }
    }

    private sealed class RunnerTestCommand(string response, bool repeat, TimeSpan repeatInterval)
        : ICommand
    {
        public string CommandName => "runner";
        public bool Repeat { get; } = repeat;
        public TimeSpan RepeatInterval { get; } = repeatInterval;
        public bool StopsShell => false;
        public int ExecuteCount { get; private set; }
        public bool ThrowOnExecute { get; set; }
        public string ExceptionMessage { get; set; } = "error";

        public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
        {
            ExecuteCount++;
            if (ThrowOnExecute)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }

            await output.WriteAsync(response, cancellationToken);
        }
    }

    private sealed class CancelAwareRunnerTestCommand : ICommand
    {
        private readonly TaskCompletionSource<bool> _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string CommandName => "runner";
        public bool Repeat => true;
        public TimeSpan RepeatInterval => TimeSpan.FromSeconds(1);
        public bool StopsShell => false;
        public bool HasEnteredExecute { get; private set; }
        public bool ThrowOperationCanceled { get; set; }

        public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
        {
            HasEnteredExecute = true;
            await _gate.Task;

            if (ThrowOperationCanceled)
            {
                throw new OperationCanceledException();
            }

            await output.WriteAsync("ok", cancellationToken);
        }

        public void ReleaseExecution()
        {
            _gate.TrySetResult(true);
        }
    }

    private sealed class IncrementalRunnerTestCommand : ICommand
    {
        private readonly TaskCompletionSource<bool> _secondChunkGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string CommandName => "runner";
        public bool Repeat => false;
        public TimeSpan RepeatInterval => TimeSpan.Zero;
        public bool StopsShell => false;

        public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
        {
            await output.WriteAsync("first", cancellationToken);
            await _secondChunkGate.Task.WaitAsync(cancellationToken);
            await output.WriteAsync(" second", cancellationToken);
        }

        public void ReleaseSecondChunk()
        {
            _secondChunkGate.TrySetResult(true);
        }
    }

    private sealed class DeferredChunkStream(Task secondChunkReady) : Stream
    {
        private readonly byte[] _firstChunk = Encoding.UTF8.GetBytes("first");
        private readonly byte[] _secondChunk = Encoding.UTF8.GetBytes(" second");
        private int _firstPosition;
        private int _secondPosition;
        private bool _secondChunkUnlocked;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _firstChunk.Length + _secondChunk.Length;

        public override long Position
        {
            get => _firstPosition + _secondPosition;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_firstPosition < _firstChunk.Length)
            {
                var copied = Math.Min(buffer.Length, _firstChunk.Length - _firstPosition);
                _firstChunk.AsSpan(_firstPosition, copied).CopyTo(buffer.Span);
                _firstPosition += copied;
                return copied;
            }

            if (!_secondChunkUnlocked)
            {
                await secondChunkReady.WaitAsync(cancellationToken);
                _secondChunkUnlocked = true;
            }

            if (_secondPosition >= _secondChunk.Length)
            {
                return 0;
            }

            var secondCopied = Math.Min(buffer.Length, _secondChunk.Length - _secondPosition);
            _secondChunk.AsSpan(_secondPosition, secondCopied).CopyTo(buffer.Span);
            _secondPosition += secondCopied;
            return secondCopied;
        }

        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}