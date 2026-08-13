using System.IO;
using System.Text;
using flconsole.Models;

namespace flconsole.Commands;

public sealed class MonitorCommand(XmlRpcClient client) : ICommand<IReadOnlyList<string>>
{
    public string CommandName => "monitor";
    public bool Repeat => true;
    public TimeSpan RepeatInterval => TimeSpan.FromSeconds(1);
    public bool StopsShell => false;

    public async Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        try
        {
            var response = await client.SendAsync(new XmlRpcRequest
            {
                MethodName = "rx.get_data",
                Parameters = []
            });

            var payload = response.Value;
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
