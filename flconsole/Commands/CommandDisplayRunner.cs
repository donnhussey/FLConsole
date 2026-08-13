using System.IO;
using System.Text;
using flconsole.Commands;

namespace flconsole;

public sealed class CommandDisplayRunner(IRenderer Renderer, ConsoleOutputBuffer OutputBuffer, IPromptState PromptState)
{
    private CancellationTokenSource? _displayLoopCancellationSource;
    private Task? _displayLoopTask;

    public async Task StartAsync(ICommand<IReadOnlyList<string>> command, IReadOnlyList<string> request)
    {
        await StopAsync();

        _displayLoopCancellationSource = new CancellationTokenSource();
        var cancellationToken = _displayLoopCancellationSource.Token;
        _displayLoopTask = Task.Run(() => RunDisplayLoopAsync(command, request, cancellationToken));
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
        _displayLoopCancellationSource = null;
        _displayLoopTask = null;
    }

    public async Task RunToCompletionAsync(ICommand<IReadOnlyList<string>> command, IReadOnlyList<string> request)
    {
        await StopAsync();
        await RunDisplayLoopAsync(command, request, CancellationToken.None);
    }

    public void AppendLineAndRender(string line)
    {
        OutputBuffer.AddLine(line);
        RenderCurrentState();
    }

    public void AppendTextAndRender(string text)
    {
        OutputBuffer.AppendText(text);
        RenderCurrentState();
    }

    private async Task RunDisplayLoopAsync(ICommand<IReadOnlyList<string>> command, IReadOnlyList<string> request, CancellationToken cancellationToken)
    {
        var buffer = new char[256];

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var stream = await command.ExecuteAsync(request);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                while (true)
                {
                    var readCount = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (readCount == 0)
                    {
                        break;
                    }

                    AppendTextAndRender(new string(buffer, 0, readCount));
                }

                if (!command.Repeat)
                {
                    AppendTextAndRender(Environment.NewLine);
                    break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                AppendLineAndRender($"Error: {ex.Message}");
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
        Renderer.RenderOutput(OutputBuffer);
        Renderer.RenderInput(PromptState.CurrentText, PromptState.CurrentCursorIndex);
    }
}