using System.Globalization;
using System.IO;
using flconsole.Models;

namespace flconsole.Commands;

public sealed class IdentifyCommand(XmlRpcClient client, IdentifyCommandSettings? settings = null) : ICommand<IReadOnlyList<string>>
{
    private const double MinCarrierOffsetHz = 1;
    private const double MaxCarrierOffsetHz = 3000;
    private const double ModemCarrierOffset = 1500;
    private const int DefaultRsidListenSeconds = 5;
    private const int DefaultTopCandidates = 5;
    private const double MinimumQualityToIdentify = 5;
    private static readonly TimeSpan FrequencyCarrierSettleDelay = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan RsidSampleInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ModeSettleDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan HeuristicQualitySampleDelay = TimeSpan.FromMilliseconds(1500);
    private readonly IdentifyCommandSettings _settings = settings ?? new IdentifyCommandSettings([]);

    public string CommandName => "identify";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        if (!TryParseRequest(request, out var useAllModems, out var listenSeconds, out var topCandidates, out var verbose))
        {
            return CommandTextStream.Create("Usage: identify [all] [listen-seconds] [top-candidates] [v]");
        }

        return CommandTextStream.Create(async output =>
        {
            var originalModemName = string.Empty;
            var originalRsidEnabled = false;

            try
            {
                originalModemName = await GetStringValueAsync("modem.get_name");
                originalRsidEnabled = await GetBooleanValueAsync("main.get_rsid");

                await SendRequestAsync("rig.take_control");
                var currentDialFrequency = await GetDoubleValueAsync("rig.get_frequency");
                var currentCarrierOffset = await GetDoubleValueAsync("modem.get_carrier");
                var signalFrequency = currentDialFrequency + currentCarrierOffset;
                var centeredDialFrequency = signalFrequency - ModemCarrierOffset;

                await SetFrequencyAndCarrierAsync(centeredDialFrequency, ModemCarrierOffset);

                await output.WriteLineAsync($"Current modem: {originalModemName}");
                await output.WriteLineAsync($"Signal frequency: {signalFrequency.ToString("0.###", CultureInfo.InvariantCulture)} Hz");
                await output.WriteLineAsync($"Centered dial frequency: {centeredDialFrequency.ToString("0.###", CultureInfo.InvariantCulture)} Hz");
                await output.WriteLineAsync($"Listening for RSID for {listenSeconds} second(s)...");

                if (!originalRsidEnabled)
                {
                    await SendRequestAsync("main.set_rsid", true);
                }

                var rsidResult = await TryIdentifyByRsidAsync(originalModemName, listenSeconds);
                if (rsidResult is not null)
                {
                    await output.WriteAsync($"RSID identified modem: {rsidResult.ModemName} (quality={rsidResult.Quality.ToString("0.###", CultureInfo.InvariantCulture)})");
                    return;
                }

                var currentQuality = await GetDoubleValueAsync("modem.get_quality");
                if (currentQuality < MinimumQualityToIdentify)
                {
                    await output.WriteAsync($"nothing to identify, quality was {currentQuality.ToString("0.###", CultureInfo.InvariantCulture)}");
                    return;
                }

                await output.WriteLineAsync("No RSID modem switch detected; running heuristic modem sweep.");
                var availableModems = await GetStringArrayValueAsync("modem.get_names");
                var modemCandidates = ResolveModemCandidates(availableModems, useAllModems);
                var candidates = await RankCandidatesAsync(modemCandidates, topCandidates, verbose, output);

                if (candidates.Count == 0)
                {
                    await output.WriteAsync("No modem candidates were available from FLDigi.");
                    return;
                }

                await output.WriteLineAsync("Top candidates:");
                for (var index = 0; index < candidates.Count; index++)
                {
                    var candidate = candidates[index];
                    await output.WriteLineAsync($"  {index + 1}. {candidate.ModemName} score={candidate.Score.ToString("0.###", CultureInfo.InvariantCulture)} quality={candidate.Quality.ToString("0.###", CultureInfo.InvariantCulture)}");
                }

                var bestCandidate = candidates[0];
                await SendRequestAsync("modem.set_by_name", bestCandidate.ModemName);
                await output.WriteAsync($"Selected modem: {bestCandidate.ModemName}");
            }
            catch (Exception ex)
            {
                await output.WriteAsync($"Error: {ex.Message}");
            }
            finally
            {
                try
                {
                    await SendRequestAsync("main.set_rsid", originalRsidEnabled);
                }
                catch
                {
                }

                if (!string.IsNullOrWhiteSpace(originalModemName))
                {
                    try
                    {
                        await SendRequestAsync("modem.set_by_name", originalModemName);
                    }
                    catch
                    {
                    }
                }
            }
        });
    }

    private static bool TryParseRequest(IReadOnlyList<string> request, out bool useAllModems, out int listenSeconds, out int topCandidates, out bool verbose)
    {
        useAllModems = false;
        listenSeconds = DefaultRsidListenSeconds;
        topCandidates = DefaultTopCandidates;
        verbose = false;

        if (request.Count > 4)
        {
            return false;
        }

        var numericValues = new List<int>(capacity: 2);
        foreach (var token in request)
        {
            if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (useAllModems)
                {
                    return false;
                }

                useAllModems = true;
                continue;
            }

            if (string.Equals(token, "v", StringComparison.OrdinalIgnoreCase))
            {
                if (verbose)
                {
                    return false;
                }

                verbose = true;
                continue;
            }

            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) || parsedValue <= 0)
            {
                return false;
            }

            numericValues.Add(parsedValue);
        }

        if (numericValues.Count > 2)
        {
            return false;
        }

        if (numericValues.Count >= 1)
        {
            listenSeconds = numericValues[0];
        }

        if (numericValues.Count == 2)
        {
            topCandidates = numericValues[1];
        }

        return true;
    }

    private async Task<RsidDetectionResult?> TryIdentifyByRsidAsync(string initialModemName, int listenSeconds)
    {
        var sampleCount = Math.Max(1, (int)Math.Ceiling(listenSeconds * (1000d / RsidSampleInterval.TotalMilliseconds)));
        for (var sample = 0; sample < sampleCount; sample++)
        {
            if (sample > 0)
            {
                await Task.Delay(RsidSampleInterval);
            }

            var currentModem = await GetStringValueAsync("modem.get_name");
            var quality = await GetDoubleValueAsync("modem.get_quality");
            if (!string.Equals(currentModem, initialModemName, StringComparison.OrdinalIgnoreCase) && quality > 0)
            {
                return new RsidDetectionResult(currentModem, quality);
            }
        }

        return null;
    }

    private async Task SetFrequencyAndCarrierAsync(double dialFrequency, double carrierOffset)
    {
        var validCarrierOffset = EnsureValidCarrierOffset(carrierOffset);

        await SendRequestAsync("rig.set_frequency", dialFrequency);
        await Task.Delay(FrequencyCarrierSettleDelay);
        await SendRequestAsync("modem.set_carrier", validCarrierOffset);
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

    private async Task<List<ModeCandidate>> RankCandidatesAsync(IReadOnlyList<string> modemCandidates, int topCandidates, bool verbose, CommandStreamBuffer output)
    {
        var candidates = new List<ModeCandidate>();

        foreach (var modemName in modemCandidates)
        {
            await SendRequestAsync("modem.set_by_name", modemName);
            await Task.Delay(ModeSettleDelay);
            await Task.Delay(HeuristicQualitySampleDelay);

            var quality = await GetDoubleValueAsync("modem.get_quality");
            var rxText = await GetRxTextAsync();
            var textSignalScore = GetTextSignalScore(rxText);
            var score = quality + textSignalScore;
            candidates.Add(new ModeCandidate(modemName, score, quality));

            if (verbose)
            {
                await output.WriteLineAsync($"Verbose candidate: {modemName} quality={quality.ToString("0.###", CultureInfo.InvariantCulture)} text={textSignalScore.ToString("0.###", CultureInfo.InvariantCulture)} score={score.ToString("0.###", CultureInfo.InvariantCulture)}");
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Quality)
            .Take(topCandidates)
            .ToList();
    }

    private IReadOnlyList<string> ResolveModemCandidates(IReadOnlyList<string> availableModems, bool useAllModems)
    {
        var normalizedAvailable = availableModems
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (useAllModems)
        {
            return normalizedAvailable;
        }

        var configuredModems = _settings.Modems
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (configuredModems.Count == 0)
        {
            return normalizedAvailable;
        }

        var availableLookup = new HashSet<string>(normalizedAvailable, StringComparer.OrdinalIgnoreCase);
        return configuredModems
            .Where(availableLookup.Contains)
            .ToList();
    }

    private async Task<string> GetRxTextAsync()
    {
        var response = await SendRequestAsync("rx.get_data");
        return response.Value switch
        {
            null => string.Empty,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => XmlRpcValueHelper.FormatValue(response.Value)
        };
    }

    private static double GetTextSignalScore(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return 0;
        }

        var printable = trimmed.Count(ch => char.IsLetterOrDigit(ch) || char.IsPunctuation(ch) || char.IsWhiteSpace(ch));
        var printableRatio = printable / (double)trimmed.Length;
        return printableRatio * 20;
    }

    private async Task<string> GetStringValueAsync(string methodName)
    {
        var response = await SendRequestAsync(methodName);
        return CommandRpcValueReader.ReadStringOrThrow(response.Value, methodName);
    }

    private async Task<double> GetDoubleValueAsync(string methodName)
    {
        var response = await SendRequestAsync(methodName);
        return CommandRpcValueReader.ReadDoubleOrThrow(response.Value, methodName);
    }

    private async Task<bool> GetBooleanValueAsync(string methodName)
    {
        var response = await SendRequestAsync(methodName);
        return CommandRpcValueReader.ReadBooleanOrThrow(response.Value, methodName);
    }

    private async Task<IReadOnlyList<string>> GetStringArrayValueAsync(string methodName)
    {
        var response = await SendRequestAsync(methodName);
        return CommandRpcValueReader.ReadStringListOrThrow(response.Value, methodName);
    }

    private Task<XmlRpcResponse> SendRequestAsync(string methodName, params object[] parameters)
    {
        return client.SendAsync(new XmlRpcRequest
        {
            MethodName = methodName,
            Parameters = parameters.Cast<object?>().ToList()
        });
    }

    private sealed record ModeCandidate(string ModemName, double Score, double Quality);

    private sealed record RsidDetectionResult(string ModemName, double Quality);
}