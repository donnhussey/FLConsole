using flconsole.Console;
using flconsole.Commands;

namespace flconsole;

internal sealed class CommandExecutor(IConsoleDisplay display, ICommandSource commandSource, CommandMessages messages) : ICommandOutput
{
    private CancellationTokenSource? _displayLoopCancellationSource;
    private CancellationTokenSource? _linkedCancellationSource;
    private Task? _displayLoopTask;

    public async Task StartAsync(ICommand command, IReadOnlyList<string> request, CancellationToken cancellationToken = default)
    {
        await StopAsync();

        _displayLoopCancellationSource = new CancellationTokenSource();
        _linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_displayLoopCancellationSource.Token, cancellationToken);
        _displayLoopTask = Task.Run(() => RunDisplayLoopAsync(command, request, _linkedCancellationSource.Token));
    }

    public async Task StopAsync()
    {
        if (_displayLoopCancellationSource is null)
        {
            return;
        }

        _displayLoopCancellationSource.Cancel();
        if (_displayLoopTask is not null)
        {
            await _displayLoopTask;
        }

        _displayLoopCancellationSource.Dispose();
        _linkedCancellationSource?.Dispose();
        _displayLoopCancellationSource = null;
        _linkedCancellationSource = null;
        _displayLoopTask = null;
    }

    public async Task RunToCompletionAsync(ICommand command, IReadOnlyList<string> request, CancellationToken cancellationToken = default)
    {
        await StopAsync();
        await RunDisplayLoopAsync(command, request, cancellationToken);
    }

    public ValueTask WriteAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        display.AppendText(text);
        RenderCurrentState();
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteLineAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        display.AppendLine(text);
        RenderCurrentState();
        return ValueTask.CompletedTask;
    }

    private async Task RunDisplayLoopAsync(ICommand command, IReadOnlyList<string> request, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await command.ExecuteAsync(request, this, cancellationToken);

                if (!command.Repeat)
                {
                    await WriteAsync(Environment.NewLine, cancellationToken);
                    break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                await WriteLineAsync(string.Format(messages.ExecutionErrorFormat, ex.Message), cancellationToken);
                break;
            }

            try
            {
                await Task.Delay(command.RepeatInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void RenderCurrentState()
    {
        var prompt = commandSource.PromptState;
        display.ShowPrompt(prompt.Text, prompt.CursorIndex);
    }
}