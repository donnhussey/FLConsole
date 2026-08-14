using System.Globalization;
using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public sealed class ScanCommand(FLDigi _fldigi, ScanCommandSettings? settings = null, CommandMessages? messages = null) : ICommand
{
    private readonly CommandMessages _messages = messages ?? CommandMessages.Defaults;
    private static readonly NumberFormatInfo DotGroupedIntegerFormat = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalDigits = 0
    };

    private readonly ScanCommandSettings _settings = settings ?? new(ScanCommandSettings.DefaultSettleDelayMilliseconds);
    private TimeSpan FrequencySettleDelay => TimeSpan.FromMilliseconds(Math.Max(0, _settings.SettleDelayMilliseconds));

    public string CommandName => "scan";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        if (!TryParseRequest(request, out var qualityThreshold))
        {
            await output.WriteAsync(_messages.ScanUsage, cancellationToken); return;
        }

        await output.WriteAsync(Environment.NewLine);

            ScanSessionState? originalState = null;

            try
            {
                await TakeControlAsync();
                originalState = await CaptureOriginalStateAsync();
                if (!string.IsNullOrWhiteSpace(_settings.ScanModemName))
                {
                    await SetModemByNameAsync(_settings.ScanModemName);
                    await Task.Delay(FrequencySettleDelay, cancellationToken);
                }

                await SweepCarrierOffsetsAsync(output, originalState.DialFrequencyHz, qualityThreshold, _settings.Debug, cancellationToken);

                await output.WriteAsync(_messages.ScanDone);
            }
            finally
            {
                if (originalState is not null)
                {
                    await RestoreOriginalStateAsync(originalState);
                }
            }
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

    private async Task SweepCarrierOffsetsAsync(ICommandOutput output, double dialFrequency, double qualityThreshold, bool debugMode, CancellationToken cancellationToken)
    {
        for (var carrierOffset = _settings.LowerCarrierOffsetHz; carrierOffset <= _settings.UpperCarrierOffsetHz; carrierOffset += _settings.CarrierStepHz)
        {
            var validCarrierOffset = EnsureValidCarrierOffset(carrierOffset);
            await _fldigi.Modem.SetCarrierAsync(ToCarrierOffsetInt(validCarrierOffset));

            await Task.Delay(FrequencySettleDelay, cancellationToken);

            if (debugMode)
            {
                var carrierReadback = EnsureValidCarrierOffset(await _fldigi.Modem.GetCarrierAsync());
                await output.WriteLineAsync(string.Format(CultureInfo.InvariantCulture, _messages.ScanCarrierDebug, validCarrierOffset, carrierReadback));
            }

            var quality = await _fldigi.Modem.GetQualityAsync();
            var reportedFrequency = dialFrequency + validCarrierOffset;
            if (debugMode)
            {
                await output.WriteLineAsync(string.Format(CultureInfo.InvariantCulture, _messages.ScanQualityDebug, FormatFrequencyWithDotSeparators(reportedFrequency), quality));
            }

            if (quality > qualityThreshold)
            {
                await output.WriteLineAsync(string.Format(CultureInfo.InvariantCulture, _messages.ScanActivity, FormatFrequencyWithDotSeparators(reportedFrequency), quality));
            }
        }
    }

    private static string FormatFrequencyWithDotSeparators(double frequency)
    {
        var roundedFrequency = Math.Round(frequency, MidpointRounding.AwayFromZero);
        return roundedFrequency.ToString("N0", DotGroupedIntegerFormat);
    }

    private bool TryParseRequest(IReadOnlyList<string> request, out double qualityThreshold)
    {
        qualityThreshold = _settings.DefaultQualityThreshold;
        var thresholdProvided = false;

        if (request.Count > 1)
        {
            return false;
        }

        foreach (var token in request)
        {
            if (CommandArguments.TryGetNonNegativeDouble(token, out var parsedThreshold))
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

    private bool IsCarrierOffsetInRange(double carrierOffset)
    {
        return carrierOffset >= _settings.MinCarrierOffsetHz && carrierOffset <= _settings.MaxCarrierOffsetHz;
    }

    private double EnsureValidCarrierOffset(double carrierOffset)
    {
        if (carrierOffset < _settings.MinCarrierOffsetHz || carrierOffset > _settings.MaxCarrierOffsetHz)
        {
            throw new InvalidOperationException($"Carrier offset must be between {_settings.MinCarrierOffsetHz.ToString("0", CultureInfo.InvariantCulture)} and {_settings.MaxCarrierOffsetHz.ToString("0", CultureInfo.InvariantCulture)} Hz.");
        }

        return carrierOffset;
    }

    private sealed record ScanSessionState(string ModemName, double DialFrequencyHz, double CarrierOffsetHz);
}
