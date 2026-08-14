namespace flconsole;

public sealed record ScanCommandSettings(
    int SettleDelayMilliseconds,
    double MinCarrierOffsetHz = 1,
    double MaxCarrierOffsetHz = 3000,
    double LowerCarrierOffsetHz = 100,
    double CarrierStepHz = 100,
    double UpperCarrierOffsetHz = 2900,
    double DefaultQualityThreshold = 20,
    string ScanModemName = "",
    bool Debug = false)
{
    public const int DefaultSettleDelayMilliseconds = 250;
}