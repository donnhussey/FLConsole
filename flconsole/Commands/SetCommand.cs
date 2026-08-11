using System.Globalization;
using System.IO;
using flconsole.Models;

namespace flconsole.Commands;

public class SetCommand(XmlRpcClient client) : ICommand<IReadOnlyList<string>>
{
    public string CommandName => "set";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task<Stream> ExecuteAsync(IReadOnlyList<string> arguments)
    {
        if (arguments.Count < 3)
        {
            return CommandTextStream.Create("Usage: set <frequency> <rig-mode> <modem-name>");
        }

        try
        {
            var frequency = arguments[0];
            var rigMode = arguments[1];
            var modemName = arguments[2];

            await client.SendAsync(new XmlRpcRequest
            {
                MethodName = "rig.take_control",
                Parameters = []
            });
            await client.SendAsync(new XmlRpcRequest
            {
                MethodName = "rig.set_frequency",
                Parameters = [double.Parse(frequency, CultureInfo.InvariantCulture)]
            });
            await client.SendAsync(new XmlRpcRequest
            {
                MethodName = "rig.set_mode",
                Parameters = [rigMode]
            });
            await client.SendAsync(new XmlRpcRequest
            {
                MethodName = "modem.set_by_name",
                Parameters = [modemName]
            });

            return CommandTextStream.Create($"Set frequency={frequency}, rigMode={rigMode}, modem={modemName}");
        }
        catch (Exception ex)
        {
            return CommandTextStream.Create($"Error: {ex.Message}");
        }
    }
}