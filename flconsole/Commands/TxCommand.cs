namespace flconsole.Commands;

public sealed class TxCommand(FLDigi fldigi, TxIdentityState identityState, TxCommandSettings settings, CommandMessages? messages = null) : ITxIdentityRequiredCommand
{
    private readonly CommandMessages _messages = messages ?? CommandMessages.Defaults;
    private TimeSpan PollInterval => TimeSpan.FromMilliseconds(Math.Max(1, settings.PollIntervalMilliseconds));

    public string CommandName => "tx";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task ExecuteAsync(IReadOnlyList<string> arguments, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        if (arguments.Count == 0)
        {
            await output.WriteAsync(_messages.TxUsage, cancellationToken);
            return;
        }

        var transmittedText = string.Join(' ', arguments) + $" de {identityState.Callsign}";
        var payload = transmittedText + "^r";
        if (await fldigi.Main.GetLockAsync())
        {
            await output.WriteAsync(_messages.TxLocked, cancellationToken);
            return;
        }

        await fldigi.Text.ClearTxAsync();
        await fldigi.Text.AddTxAsync(payload);
        await fldigi.Main.TxAsync();
        await output.WriteLineAsync(_messages.TxStarted, cancellationToken);
        await output.WriteLineAsync(transmittedText, cancellationToken);

        while (await fldigi.Main.GetLockAsync())
        {
            await Task.Delay(PollInterval, cancellationToken);
        }

        //await fldigi.Text.ClearTxAsync();
        await output.WriteLineAsync(_messages.TxDone, cancellationToken);
    }
}
