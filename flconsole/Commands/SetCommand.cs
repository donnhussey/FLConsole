using System.Globalization;
using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public class SetCommand(FLDigi _fldigi, FrequencyCommandSettings? commandSettings = null, CommandMessages? messages = null) : ICommand
{
    private readonly FrequencyCommandSettings _settings = commandSettings ?? new(1, 3000, 1500, 150);
    private readonly FrequencyTuner _tuner = new(_fldigi, commandSettings ?? new(1, 3000, 1500, 150));
    private readonly CommandMessages _messages = messages ?? CommandMessages.Defaults;

    public string CommandName => "set";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task ExecuteAsync(IReadOnlyList<string> arguments, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        if (arguments.Count is < 1 or > 3)
        {
            await output.WriteAsync(_messages.SetUsage, cancellationToken); return;
        }

        var frequency = arguments[0];
        var modemName = arguments.Count > 1 ? arguments[1] : string.Empty;
        var rigMode = arguments.Count > 2 ? arguments[2] : string.Empty;

        await _fldigi.Rig.TakeControlAsync();
        if (!CommandArguments.TryGetFrequency(arguments, 0, out var requestedFrequency))
        {
            await output.WriteAsync(_messages.SetUsage, cancellationToken); return;
        }

        await _tuner.SetAsync(requestedFrequency - _settings.CenterCarrierOffsetHz, _settings.CenterCarrierOffsetHz, cancellationToken);
        if (!string.IsNullOrWhiteSpace(modemName))
        {
            await _fldigi.Modem.SetByNameAsync(modemName);
        }

        if (!string.IsNullOrWhiteSpace(rigMode))
        {
            await _fldigi.Rig.SetModeAsync(rigMode);
        }

        await output.WriteAsync(string.Format(_messages.SetResult, frequency, modemName, rigMode), cancellationToken);
    }

}