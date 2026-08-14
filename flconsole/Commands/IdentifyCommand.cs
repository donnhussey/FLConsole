using System.Globalization;
using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public sealed class IdentifyCommand(FLDigi _fldigi, IdentifyCommandSettings? settings = null, CommandMessages? messages = null, FrequencyCommandSettings? frequencySettings = null) : ICommand
{
    private readonly IdentifyCommandSettings _settings = settings ?? new IdentifyCommandSettings([]);
    private readonly CommandMessages _messages = messages ?? CommandMessages.Defaults;
    private readonly FrequencyTuner _tuner = new(_fldigi, frequencySettings ?? new FrequencyCommandSettings(1, 3000, 1500, 150));
    private TimeSpan RsidSampleInterval => TimeSpan.FromMilliseconds(_settings.RsidSampleIntervalMilliseconds);
    private TimeSpan ModeSettleDelay => TimeSpan.FromMilliseconds(_settings.ModeSettleDelayMilliseconds);
    private TimeSpan HeuristicQualitySampleDelay => TimeSpan.FromMilliseconds(_settings.HeuristicQualitySampleDelayMilliseconds);

    public string CommandName => "identify";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        if (!TryParseRequest(request, out var useAllModems, out var listenSeconds, out var topCandidates, out var verbose))
        {
            await output.WriteAsync(_messages.IdentifyUsage, cancellationToken); return;
        }

        var originalModemName = string.Empty;
        var originalRsidEnabled = false;

        try
        {
                originalModemName = await _fldigi.Modem.GetNameAsync();
                originalRsidEnabled = await _fldigi.Main.GetRsidAsync();

                await _fldigi.Rig.TakeControlAsync();
                var currentDialFrequency = await _fldigi.Rig.GetFrequencyAsync();
                var currentCarrierOffset = await _fldigi.Modem.GetCarrierAsync();
                var signalFrequency = currentDialFrequency + currentCarrierOffset;
                var centeredDialFrequency = signalFrequency - _settings.ModemCarrierOffset;

                await _tuner.SetAsync(centeredDialFrequency, _settings.ModemCarrierOffset, cancellationToken);

                await output.WriteLineAsync(string.Format(_messages.IdentifyCurrentModem, originalModemName));
                await output.WriteLineAsync(string.Format(CultureInfo.InvariantCulture, _messages.IdentifySignalFrequency, signalFrequency));
                await output.WriteLineAsync(string.Format(CultureInfo.InvariantCulture, _messages.IdentifyCenteredFrequency, centeredDialFrequency));
                await output.WriteLineAsync(string.Format(_messages.IdentifyListening, listenSeconds));

                if (!originalRsidEnabled)
                {
                    await _fldigi.Main.SetRsidAsync(true);
                }

                var rsidResult = await TryIdentifyByRsidAsync(originalModemName, listenSeconds, cancellationToken);
                if (rsidResult is not null)
                {
                    await output.WriteAsync(string.Format(CultureInfo.InvariantCulture, _messages.IdentifyRsidResult, rsidResult.ModemName, rsidResult.Quality));
                    return;
                }

                var currentQuality = await _fldigi.Modem.GetQualityAsync();
                if (currentQuality < _settings.MinimumQualityToIdentify)
                {
                    await output.WriteAsync(string.Format(CultureInfo.InvariantCulture, _messages.IdentifyNothing, currentQuality));
                    return;
                }

                await output.WriteLineAsync("No RSID modem switch detected; running heuristic modem sweep.");
                var availableModems = await _fldigi.Modem.GetNamesAsync();
                var modemCandidates = ResolveModemCandidates(availableModems, useAllModems);
                var candidates = await RankCandidatesAsync(modemCandidates, topCandidates, verbose, output, cancellationToken);

                if (candidates.Count == 0)
                {
                    await output.WriteAsync(_messages.IdentifyNoCandidates);
                    return;
                }

                await output.WriteLineAsync(_messages.IdentifyTopCandidates);
                for (var index = 0; index < candidates.Count; index++)
                {
                    var candidate = candidates[index];
                    await output.WriteLineAsync(string.Format(CultureInfo.InvariantCulture, _messages.IdentifyCandidate, index + 1, candidate.ModemName, candidate.Score, candidate.Quality));
                }

                var bestCandidate = candidates[0];
                await _fldigi.Modem.SetByNameAsync(bestCandidate.ModemName);
                await output.WriteAsync(string.Format(_messages.IdentifySelected, bestCandidate.ModemName));
        }
        finally
        {
                try
                {
                    await _fldigi.Main.SetRsidAsync(originalRsidEnabled);
                }
                catch
                {
                }

                if (!string.IsNullOrWhiteSpace(originalModemName))
                {
                    try
                    {
                        await _fldigi.Modem.SetByNameAsync(originalModemName);
                    }
                    catch
                    {
                    }
                }
        }
    }

    private bool TryParseRequest(IReadOnlyList<string> request, out bool useAllModems, out int listenSeconds, out int topCandidates, out bool verbose)
    {
        useAllModems = false;
        listenSeconds = _settings.DefaultRsidListenSeconds;
        topCandidates = _settings.DefaultTopCandidates;
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

            if (!CommandArguments.TryGetPositiveInt(token, out var parsedValue))
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

    private async Task<RsidDetectionResult?> TryIdentifyByRsidAsync(string initialModemName, int listenSeconds, CancellationToken cancellationToken)
    {
        var sampleCount = Math.Max(1, (int)Math.Ceiling(listenSeconds * (1000d / RsidSampleInterval.TotalMilliseconds)));
        for (var sample = 0; sample < sampleCount; sample++)
        {
            if (sample > 0)
            {
                await Task.Delay(RsidSampleInterval, cancellationToken);
            }

            var currentModem = await _fldigi.Modem.GetNameAsync();
            var quality = await _fldigi.Modem.GetQualityAsync();
            if (!string.Equals(currentModem, initialModemName, StringComparison.OrdinalIgnoreCase) && quality > 0)
            {
                return new RsidDetectionResult(currentModem, quality);
            }
        }

        return null;
    }

    private async Task<List<ModeCandidate>> RankCandidatesAsync(IReadOnlyList<string> modemCandidates, int topCandidates, bool verbose, ICommandOutput output, CancellationToken cancellationToken)
    {
        var candidates = new List<ModeCandidate>();

        foreach (var modemName in modemCandidates)
        {
            await _fldigi.Modem.SetByNameAsync(modemName);
            await Task.Delay(ModeSettleDelay, cancellationToken);
            await Task.Delay(HeuristicQualitySampleDelay, cancellationToken);

            var quality = await _fldigi.Modem.GetQualityAsync();
            var rxText = await GetRxTextAsync();
            var textSignalScore = GetTextSignalScore(rxText);
            var score = quality + textSignalScore;
            candidates.Add(new ModeCandidate(modemName, score, quality));

            if (verbose)
            {
                await output.WriteLineAsync(string.Format(CultureInfo.InvariantCulture, _messages.IdentifyVerboseCandidate, modemName, quality, textSignalScore, score));
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
        var response = await _fldigi.Rx.GetDataAsync();
        return response switch
        {
            null => string.Empty,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => XmlRpcValueHelper.FormatValue(response)
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

    private sealed record ModeCandidate(string ModemName, double Score, double Quality);

    private sealed record RsidDetectionResult(string ModemName, double Quality);
}