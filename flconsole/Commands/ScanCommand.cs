using System.Globalization;
using System.IO;
using flconsole.Models;

namespace flconsole.Commands;

public sealed class ScanCommand(XmlRpcClient client, ScanCommandSettings? settings = null) : ICommand<IReadOnlyList<string>>
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
        await SendRequestAsync("rig.take_control");
    }

    private async Task<ScanSessionState> CaptureOriginalStateAsync()
    {
        var modemName = await GetStringValueAsync("modem.get_name");
        var dialFrequency = await GetDoubleValueAsync("rig.get_frequency");
        var carrierOffset = await GetDoubleValueAsync("modem.get_carrier");
        return new ScanSessionState(modemName, dialFrequency, carrierOffset);
    }

    private async Task RestoreOriginalStateAsync(ScanSessionState state)
    {
        if (!string.IsNullOrWhiteSpace(state.ModemName))
        {
            await TrySendRequestAsync("modem.set_by_name", state.ModemName);
        }

        await TrySendRequestAsync("rig.set_frequency", state.DialFrequencyHz);

        if (IsCarrierOffsetInRange(state.CarrierOffsetHz))
        {
            await TrySendRequestAsync("modem.set_carrier", ToCarrierOffsetInt(state.CarrierOffsetHz));
        }
    }

    private async Task SetModemByNameAsync(string modemName)
    {
        await SendRequestAsync("modem.set_by_name", modemName);
    }

    private async Task SweepCarrierOffsetsAsync(CommandStreamBuffer output, double dialFrequency, double qualityThreshold, bool debugMode)
    {
        for (var carrierOffset = LowerCarrierOffsetHz; carrierOffset <= UpperCarrierOffsetHz; carrierOffset += CarrierStepHz)
        {
            var validCarrierOffset = EnsureValidCarrierOffset(carrierOffset);
            await SendRequestAsync("modem.set_carrier", ToCarrierOffsetInt(validCarrierOffset));

            await Task.Delay(_frequencySettleDelay);

            if (debugMode)
            {
                var carrierReadback = EnsureValidCarrierOffset(await GetDoubleValueAsync("modem.get_carrier"));
                await output.WriteLineAsync($"Carrier requested={validCarrierOffset.ToString("0.###", CultureInfo.InvariantCulture)} Hz readback={carrierReadback.ToString("0.###", CultureInfo.InvariantCulture)} Hz");
            }

            var quality = await GetDoubleValueAsync("modem.get_quality");
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

    private async Task<double> GetDoubleValueAsync(string methodName)
    {
        var response = await SendRequestAsync(methodName);
        return CommandRpcValueReader.ReadDoubleOrThrow(response.Value, methodName);
    }

    private async Task<string> GetStringValueAsync(string methodName)
    {
        var response = await SendRequestAsync(methodName);
        return CommandRpcValueReader.ReadStringOrThrow(response.Value, methodName);
    }

    private async Task TrySendRequestAsync(string methodName, params object[] parameters)
    {
        try
        {
            await SendRequestAsync(methodName, parameters);
        }
        catch
        {
        }
    }

    private async Task<XmlRpcResponse> SendRequestAsync(string methodName, params object[] parameters)
    {
        return await client.SendAsync(new XmlRpcRequest
        {
            MethodName = methodName,
            Parameters = [.. parameters]
        });
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
