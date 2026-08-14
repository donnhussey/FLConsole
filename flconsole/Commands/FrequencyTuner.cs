using System.Globalization;

namespace flconsole.Commands;

internal sealed class FrequencyTuner(FLDigi fldigi, FrequencyCommandSettings settings)
{
    private readonly TimeSpan _settleDelay = TimeSpan.FromMilliseconds(settings.SettleDelayMilliseconds);

    public async Task SetAsync(double dialFrequency, double carrierOffset, CancellationToken cancellationToken = default)
    {
        EnsureValidCarrierOffset(carrierOffset);
        await fldigi.Rig.SetFrequencyAsync(dialFrequency);
        await Task.Delay(_settleDelay, cancellationToken);
        await SetCarrierAsync(carrierOffset, cancellationToken);
    }

    public async Task SetCarrierAsync(double carrierOffset, CancellationToken cancellationToken = default)
    {
        EnsureValidCarrierOffset(carrierOffset);
        await fldigi.Modem.SetCarrierAsync(carrierOffset);
        await Task.Delay(_settleDelay, cancellationToken);
    }

    private void EnsureValidCarrierOffset(double carrierOffset)
    {
        if (carrierOffset < settings.MinCarrierOffsetHz || carrierOffset > settings.MaxCarrierOffsetHz)
        {
            throw new InvalidOperationException($"Carrier offset must be between {settings.MinCarrierOffsetHz.ToString("0", CultureInfo.InvariantCulture)} and {settings.MaxCarrierOffsetHz.ToString("0", CultureInfo.InvariantCulture)} Hz.");
        }
    }
}
