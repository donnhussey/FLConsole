namespace flconsole;

public sealed record ScanCommandSettings(int SettleDelayMilliseconds)
{
    public const int DefaultSettleDelayMilliseconds = 250;
}