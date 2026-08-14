namespace flconsole;

internal sealed record ConsoleSettings(string PromptPrefix, int MaxLines);

public sealed record FrequencyCommandSettings(
    double MinCarrierOffsetHz,
    double MaxCarrierOffsetHz,
    double CenterCarrierOffsetHz,
    int SettleDelayMilliseconds);

public sealed record MonitorCommandSettings(int PollIntervalMilliseconds);
