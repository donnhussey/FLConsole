using System.IO;
using System.Text;
using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public sealed class MonitorCommand(FLDigi _fldigi) : ICommand<IReadOnlyList<string>>
{
    public string CommandName => "monitor";
    public bool Repeat => true;
    public TimeSpan RepeatInterval => TimeSpan.FromSeconds(1);
    public bool StopsShell => false;

    public async Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        try
        {
            var response = await _fldigi.Rx.GetDataAsync();

            var payload = response;
            var text = payload is null
                ? "null"
                : payload is byte[] bytes
                    ? Encoding.UTF8.GetString(bytes)
                : payload is string payloadText
                    ? payloadText
                    : XmlRpcValueHelper.FormatValue(payload);

            return CommandTextStream.Create(text);
        }
        catch (Exception ex)
        {
            return CommandTextStream.Create($"Error: {ex.Message}");
        }
    }
}
