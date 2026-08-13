using System.Globalization;
using System.IO;
using flconsole.Models;

namespace flconsole.Commands;

public sealed class AdjustCommand(XmlRpcClient client) : ICommand<IReadOnlyList<string>>
{
    private const double MinCarrierOffsetHz = 1;
    private const double MaxCarrierOffsetHz = 3000;
    private const double CenterCarrierOffsetHz = 1500;
    private const double LowerCarrierOffsetHz = MinCarrierOffsetHz;
    private const double UpperCarrierOffsetHz = MaxCarrierOffsetHz;
    private static readonly TimeSpan FrequencyCarrierSettleDelay = TimeSpan.FromMilliseconds(150);

    public string CommandName => "adjust";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task<Stream> ExecuteAsync(IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1
            || !double.TryParse(arguments[0], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var targetFrequency)
            || targetFrequency <= 0)
        {
            return CommandTextStream.Create("Usage: adjust <frequency>");
        }

        try
        {
            await SendRequestAsync("rig.take_control");

            var currentDialFrequency = await GetDoubleValueAsync("rig.get_frequency");
            var currentBandLowerBound = currentDialFrequency + LowerCarrierOffsetHz;
            var currentBandUpperBound = currentDialFrequency + UpperCarrierOffsetHz;

            double resultingDialFrequency;
            double carrierOffset;

            if (targetFrequency >= currentBandLowerBound && targetFrequency <= currentBandUpperBound)
            {
                resultingDialFrequency = currentDialFrequency;
                carrierOffset = EnsureValidCarrierOffset(targetFrequency - currentDialFrequency);

                await SetCarrierAsync(carrierOffset);
            }
            else
            {
                resultingDialFrequency = targetFrequency - CenterCarrierOffsetHz;
                carrierOffset = EnsureValidCarrierOffset(CenterCarrierOffsetHz);

                await SetFrequencyAndCarrierAsync(resultingDialFrequency, carrierOffset);
            }

            return CommandTextStream.Create($"Adjusted frequency={targetFrequency.ToString("0.###", CultureInfo.InvariantCulture)}, dial={resultingDialFrequency.ToString("0.###", CultureInfo.InvariantCulture)}, carrier={carrierOffset.ToString("0.###", CultureInfo.InvariantCulture)}");
        }
        catch (Exception ex)
        {
            return CommandTextStream.Create($"Error: {ex.Message}");
        }
    }

    private async Task<double> GetDoubleValueAsync(string methodName)
    {
        var response = await SendRequestAsync(methodName);
        return CommandRpcValueReader.ReadDoubleOrThrow(response.Value, methodName);
    }

    private async Task SetFrequencyAndCarrierAsync(double dialFrequency, double carrierOffset)
    {
        await SendRequestAsync("rig.set_frequency", dialFrequency);

        await Task.Delay(FrequencyCarrierSettleDelay);
        await SetCarrierAsync(carrierOffset);
    }

    private async Task SetCarrierAsync(double carrierOffset)
    {
        var validCarrierOffset = EnsureValidCarrierOffset(carrierOffset);

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