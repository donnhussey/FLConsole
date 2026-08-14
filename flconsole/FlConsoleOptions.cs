namespace flconsole;

internal sealed record FlConsoleOptions(
    XmlRpcConnectionSettings Connection,
    ConsoleSettings Console,
    CommandMessages Messages,
    ScanCommandSettings Scan,
    IdentifyCommandSettings Identify,
    FrequencyCommandSettings Frequency,
    MonitorCommandSettings Monitor);
