using System.Text;
using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public sealed class MonitorCommand(FLDigi _fldigi, MonitorCommandSettings? settings = null, CommandMessages? messages = null) : ICommand
{
    private readonly MonitorCommandSettings _settings = settings ?? new(1000);
    private readonly CommandMessages _messages = messages ?? CommandMessages.Defaults;
    public string CommandName => "monitor";
    public bool Repeat => true;
    public TimeSpan RepeatInterval => TimeSpan.FromMilliseconds(_settings.PollIntervalMilliseconds);
    public bool StopsShell => false;

    public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        var response = await _fldigi.Rx.GetDataAsync();

            var payload = response;
            var text = payload is null
                ? _messages.MonitorNullValue
                : payload is byte[] bytes
                    ? Encoding.UTF8.GetString(bytes)
                : payload is string payloadText
                    ? payloadText
                    : XmlRpcValueHelper.FormatValue(payload);

        await output.WriteAsync(text, cancellationToken);
    }
}
