using System.Globalization;
using System.IO;
using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public sealed class ScanCommand(FLDigi _fldigi, ScanCommandSettings? settings = null) : ICommand<IReadOnlyList<string>>
{
    private static readonly NumberFormatInfo DotGroupedIntegerFormat = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalDigits = 0
    };

    private const double MinCarrierOffsetHz = 1;
    private const double MaxCarrierOffsetHz = 3000;
    private const double LowerCarrierOffsetHz = 100;
    private const double CarrierStepHz = 100;
    private const double UpperCarrierOffsetHz = 2900;
    private const double DefaultQualityThreshold = 20;
    private const string ScanModemName = "CW";
    private readonly TimeSpan _frequencySettleDelay = TimeSpan.FromMilliseconds(
        Math.Max(0, settings?.SettleDelayMilliseconds ?? ScanCommandSettings.DefaultSettleDelayMilliseconds));

    public string CommandName => "scan";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        if (!TryParseRequest(request, out var qualityThreshold, out var debugMode))
        {
            return CommandTextStream.Create("Usage: scan [quality-threshold] [debug]");
        }

        return CommandTextStream.Create(async output =>
        {
            await output.WriteAsync(Environment.NewLine);

            ScanSessionState? originalState = null;

            try
            {
                await TakeControlAsync();
                originalState = await CaptureOriginalStateAsync();
                await SetModemByNameAsync(ScanModemName);
                await Task.Delay(_frequencySettleDelay);

                await SweepCarrierOffsetsAsync(output, originalState.DialFrequencyHz, qualityThreshold, debugMode);

                await output.WriteAsync("Done.");
            }
            finally
            {
                if (originalState is not null)
                {
                    await RestoreOriginalStateAsync(originalState);
                }
            }
        });
    }

    private async Task TakeControlAsync()
    {
        await _fldigi.Rig.TakeControlAsync();
    }

    private async Task<ScanSessionState> CaptureOriginalStateAsync()
    {
        var modemName = await _fldigi.Modem.GetNameAsync();
        var dialFrequency = await _fldigi.Rig.GetFrequencyAsync();
        var carrierOffset = await _fldigi.Modem.GetCarrierAsync();
        return new ScanSessionState(modemName, dialFrequency, carrierOffset);
    }

    private async Task RestoreOriginalStateAsync(ScanSessionState state)
    {
        if (!string.IsNullOrWhiteSpace(state.ModemName))
        {
            await TrySendAsync(() => _fldigi.Modem.SetByNameAsync(state.ModemName));
        }

        await TrySendAsync(() => _fldigi.Rig.SetFrequencyAsync(state.DialFrequencyHz));

        if (IsCarrierOffsetInRange(state.CarrierOffsetHz))
        {
            await TrySendAsync(() => _fldigi.Modem.SetCarrierAsync(ToCarrierOffsetInt(state.CarrierOffsetHz)));
        }
    }

    private async Task SetModemByNameAsync(string modemName)
    {
        await _fldigi.Modem.SetByNameAsync(modemName);
    }

    private async Task SweepCarrierOffsetsAsync(CommandStreamBuffer output, double dialFrequency, double qualityThreshold, bool debugMode)
    {
        for (var carrierOffset = LowerCarrierOffsetHz; carrierOffset <= UpperCarrierOffsetHz; carrierOffset += CarrierStepHz)
        {
            var validCarrierOffset = EnsureValidCarrierOffset(carrierOffset);
            await _fldigi.Modem.SetCarrierAsync(ToCarrierOffsetInt(validCarrierOffset));

            await Task.Delay(_frequencySettleDelay);

            if (debugMode)
            {
                var carrierReadback = EnsureValidCarrierOffset(await _fldigi.Modem.GetCarrierAsync());
                await output.WriteLineAsync($"Carrier requested={validCarrierOffset.ToString("0.###", CultureInfo.InvariantCulture)} Hz readback={carrierReadback.ToString("0.###", CultureInfo.InvariantCulture)} Hz");
            }

            var quality = await _fldigi.Modem.GetQualityAsync();
            var reportedFrequency = dialFrequency + validCarrierOffset;
            if (debugMode)
            {
                await output.WriteLineAsync($"Quality at {FormatFrequencyWithDotSeparators(reportedFrequency)} Hz: {quality.ToString("0.######", CultureInfo.InvariantCulture)}");
            }

            if (quality > qualityThreshold)
            {
                await output.WriteLineAsync($"Activity at {FormatFrequencyWithDotSeparators(reportedFrequency)} Hz (quality={quality.ToString("0.###", CultureInfo.InvariantCulture)})");
            }
        }
    }

    private static string FormatFrequencyWithDotSeparators(double frequency)
    {
        var roundedFrequency = Math.Round(frequency, MidpointRounding.AwayFromZero);
        return roundedFrequency.ToString("N0", DotGroupedIntegerFormat);
    }

    private static bool TryParseRequest(IReadOnlyList<string> request, out double qualityThreshold, out bool debugMode)
    {
        qualityThreshold = DefaultQualityThreshold;
        debugMode = false;
        var thresholdProvided = false;

        if (request.Count > 2)
        {
            return false;
        }

        foreach (var token in request)
        {
            if (string.Equals(token, "debug", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "d", StringComparison.OrdinalIgnoreCase))
            {
                if (debugMode)
                {
                    return false;
                }

                debugMode = true;
                continue;
            }

            if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedThreshold)
                && parsedThreshold >= 0)
            {
                if (thresholdProvided)
                {
                    return false;
                }

                qualityThreshold = parsedThreshold;
                thresholdProvided = true;
                continue;
            }

            return false;
        }

        return true;
    }

    private static async Task TrySendAsync(Func<Task<object?>> operation)
    {
        try
        {
            await operation();
        }
        catch
        {
        }
    }

    private static int ToCarrierOffsetInt(double carrierOffset)
    {
        return (int)Math.Round(carrierOffset, MidpointRounding.AwayFromZero);
    }

    private static bool IsCarrierOffsetInRange(double carrierOffset)
    {
        return carrierOffset >= MinCarrierOffsetHz && carrierOffset <= MaxCarrierOffsetHz;
    }

    private static double EnsureValidCarrierOffset(double carrierOffset)
    {
        if (carrierOffset < MinCarrierOffsetHz || carrierOffset > MaxCarrierOffsetHz)
        {
            throw new InvalidOperationException($"Carrier offset must be between {MinCarrierOffsetHz.ToString("0", CultureInfo.InvariantCulture)} and {MaxCarrierOffsetHz.ToString("0", CultureInfo.InvariantCulture)} Hz.");
        }

        return carrierOffset;
    }

    private sealed record ScanSessionState(string ModemName, double DialFrequencyHz, double CarrierOffsetHz);
}
