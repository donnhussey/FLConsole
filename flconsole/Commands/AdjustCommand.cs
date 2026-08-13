using System.Globalization;
using System.IO;
using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public sealed class AdjustCommand(FLDigi _fldigi) : ICommand<IReadOnlyList<string>>
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
            await _fldigi.Rig.TakeControlAsync();

            var currentDialFrequency = await _fldigi.Rig.GetFrequencyAsync();
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

    private async Task SetFrequencyAndCarrierAsync(double dialFrequency, double carrierOffset)
    {
        await _fldigi.Rig.SetFrequencyAsync(dialFrequency);

        await Task.Delay(FrequencyCarrierSettleDelay);
        await SetCarrierAsync(carrierOffset);
    }

    private async Task SetCarrierAsync(double carrierOffset)
    {
        var validCarrierOffset = EnsureValidCarrierOffset(carrierOffset);

        await _fldigi.Modem.SetCarrierAsync(validCarrierOffset);

        await Task.Delay(FrequencyCarrierSettleDelay);
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