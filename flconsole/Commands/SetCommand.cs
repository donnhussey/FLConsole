using System.Globalization;
using System.IO;
using flconsole.Models;

namespace flconsole.Commands;

public class SetCommand(XmlRpcClient client) : ICommand<IReadOnlyList<string>>
{
    private const double MinCarrierOffsetHz = 1;
    private const double MaxCarrierOffsetHz = 3000;
    private const double ModemCarrierOffset = 1500;
    private static readonly TimeSpan FrequencyCarrierSettleDelay = TimeSpan.FromMilliseconds(150);

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

            await SendRequestAsync("rig.take_control");
            await SetFrequencyAndCarrierAsync(double.Parse(frequency, CultureInfo.InvariantCulture) - ModemCarrierOffset, ModemCarrierOffset);
            await SendRequestAsync("rig.set_mode", rigMode);
            await SendRequestAsync("modem.set_by_name", modemName);

            return CommandTextStream.Create($"Set frequency={frequency}, rigMode={rigMode}, modem={modemName}");
        }
        catch (Exception ex)
        {
            return CommandTextStream.Create($"Error: {ex.Message}");
        }
    }

    private async Task SetFrequencyAndCarrierAsync(double dialFrequency, double carrierOffset)
    {
        var validCarrierOffset = EnsureValidCarrierOffset(carrierOffset);

        await SendRequestAsync("rig.set_frequency", dialFrequency);

        await Task.Delay(FrequencyCarrierSettleDelay);

        await SendRequestAsync("modem.set_carrier", validCarrierOffset);

        await Task.Delay(FrequencyCarrierSettleDelay);
    }

    private Task<XmlRpcResponse> SendRequestAsync(string methodName, params object[] parameters)
    {
        return client.SendAsync(new XmlRpcRequest
        {
            MethodName = methodName,
            Parameters = [.. parameters]
        });
    }

    private static double EnsureValidCarrierOffset(double carrierOffset)
    {
        if (carrierOffset < MinCarrierOffsetHz || carrierOffset > MaxCarrierOffsetHz)
        {
            throw new InvalidOperationException($"Carrier offset must be between {MinCarrierOffsetHz.ToString("0", CultureInfo.InvariantCulture)} and {MaxCarrierOffsetHz.ToString("0", CultureInfo.InvariantCulture)} Hz.");
        }

        return carrierOffset;
    }
}