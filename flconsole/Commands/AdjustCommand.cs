using System.Globalization;
using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public sealed class AdjustCommand(FLDigi _fldigi, FrequencyCommandSettings? commandSettings = null, CommandMessages? messages = null) : ICommand
{
    private readonly FrequencyCommandSettings _settings = commandSettings ?? new(1, 3000, 1500, 150);
    private readonly FrequencyTuner _tuner = new(_fldigi, commandSettings ?? new(1, 3000, 1500, 150));
    private readonly CommandMessages _messages = messages ?? CommandMessages.Defaults;

    public string CommandName => "adjust";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task ExecuteAsync(IReadOnlyList<string> arguments, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        if (arguments.Count != 1 || !CommandArguments.TryGetFrequency(arguments, 0, out var targetFrequency))
        {
            await output.WriteAsync(_messages.AdjustUsage, cancellationToken); return;
        }

        await _fldigi.Rig.TakeControlAsync();

            var currentDialFrequency = await _fldigi.Rig.GetFrequencyAsync();
            var currentBandLowerBound = currentDialFrequency + _settings.MinCarrierOffsetHz;
            var currentBandUpperBound = currentDialFrequency + _settings.MaxCarrierOffsetHz;

            double resultingDialFrequency;
            double carrierOffset;

            if (targetFrequency >= currentBandLowerBound && targetFrequency <= currentBandUpperBound)
            {
                resultingDialFrequency = currentDialFrequency;
                carrierOffset = targetFrequency - currentDialFrequency;

                await _tuner.SetCarrierAsync(carrierOffset, cancellationToken);
            }
            else
            {
                resultingDialFrequency = targetFrequency - _settings.CenterCarrierOffsetHz;
                carrierOffset = _settings.CenterCarrierOffsetHz;

                await _tuner.SetAsync(resultingDialFrequency, carrierOffset, cancellationToken);
            }

        await output.WriteAsync(string.Format(CultureInfo.InvariantCulture, _messages.AdjustResult, targetFrequency, resultingDialFrequency, carrierOffset), cancellationToken);
    }

}