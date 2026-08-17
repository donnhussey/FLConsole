using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace flconsole;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddFlConsole(this IServiceCollection services, IConfiguration configuration, bool debug = false, bool txEnabled = false)
    {
        var (host, port) = ReadXmlRpcSettings(configuration);
        var connectionSettings = new XmlRpcConnectionSettings(host, port);
        var consoleSettings = ReadConsoleSettings(configuration);
        var commandMessages = ReadCommandMessages(configuration);
        var scanCommandSettings = ReadScanCommandSettings(configuration, debug);
        var identifyCommandSettings = ReadIdentifyCommandSettings(configuration);
        var frequencySettings = ReadFrequencyCommandSettings(configuration);
        var monitorSettings = ReadMonitorCommandSettings(configuration);
        var txSettings = ReadTxCommandSettings(configuration);
        var options = new FlConsoleOptions(connectionSettings, consoleSettings, commandMessages, scanCommandSettings, identifyCommandSettings, frequencySettings, monitorSettings, txSettings);

        services.AddSingleton(options);
        services.AddSingleton<TxIdentityState>();
        services.AddSingleton(provider => provider.GetRequiredService<FlConsoleOptions>().Connection);
        services.AddSingleton(provider => provider.GetRequiredService<FlConsoleOptions>().Console);
        services.AddSingleton(provider => provider.GetRequiredService<FlConsoleOptions>().Messages);
        services.AddSingleton(provider => provider.GetRequiredService<FlConsoleOptions>().Scan);
        services.AddSingleton(provider => provider.GetRequiredService<FlConsoleOptions>().Identify);
        services.AddSingleton(provider => provider.GetRequiredService<FlConsoleOptions>().Frequency);
        services.AddSingleton(provider => provider.GetRequiredService<FlConsoleOptions>().Monitor);
        services.AddSingleton(provider => provider.GetRequiredService<FlConsoleOptions>().Tx);
        services.AddSingleton<HttpClient>();
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<XmlRpcConnectionSettings>();
            var httpClient = provider.GetRequiredService<HttpClient>();
            return new FLDigi(settings, httpClient);
        });
        services.AddSingleton<IConsole>(provider =>
        {
            var settings = provider.GetRequiredService<FlConsoleOptions>().Console;
            return ConsoleFactory.Create(settings.PromptPrefix, settings.MaxLines);
        });
        services.AddSingleton(provider => provider.GetRequiredService<IConsole>().Display);
        services.AddSingleton(provider => provider.GetRequiredService<IConsole>().CommandSource);
        services.AddSingleton<CommandExecutor>();
        services.AddSingleton<ICommand, HelpCommand>();
        services.AddSingleton<ICommand, ClearCommand>();
        services.AddSingleton<ICommand, QuitCommand>();
        services.AddSingleton<ICommand, AdjustCommand>();
        services.AddSingleton<ICommand, SetCommand>();
        services.AddSingleton<ICommand, ScanCommand>();
        services.AddSingleton<ICommand, MonitorCommand>();
        services.AddSingleton<ICommand, IdentifyCommand>();
        if (txEnabled)
        {
            services.AddSingleton<ICommand, SetCallCommand>();
            services.AddSingleton<ICommand, TxCommand>();
        }
        if (debug)
        {
            services.AddSingleton<ICommand, MethodCallCommand>();
        }
        services.AddSingleton<ICommandResolver>(provider => new CommandResolver(provider.GetServices<ICommand>(), txEnabled, provider.GetRequiredService<TxIdentityState>()));
        services.AddSingleton<FlConsoleShellController>();
        services.AddSingleton<FlConsoleApplication>();

        return services;
    }

    private static (string Host, int Port) ReadXmlRpcSettings(IConfiguration configuration)
    {
        return (RequiredString(configuration, "FlConsole:Host"), RequiredInt(configuration, "FlConsole:Port"));
    }

    private static ScanCommandSettings ReadScanCommandSettings(IConfiguration configuration, bool debug)
    {
        return new ScanCommandSettings(
            RequiredInt(configuration, "FlConsole:Scan:SettleDelayMilliseconds"),
            RequiredDouble(configuration, "FlConsole:Scan:MinCarrierOffsetHz"),
            RequiredDouble(configuration, "FlConsole:Scan:MaxCarrierOffsetHz"),
            RequiredDouble(configuration, "FlConsole:Scan:LowerCarrierOffsetHz"),
            RequiredDouble(configuration, "FlConsole:Scan:CarrierStepHz"),
            RequiredDouble(configuration, "FlConsole:Scan:UpperCarrierOffsetHz"),
            RequiredDouble(configuration, "FlConsole:Scan:DefaultQualityThreshold"),
            RequiredString(configuration, "FlConsole:Scan:ModemName"),
            debug);
    }

    private static IdentifyCommandSettings ReadIdentifyCommandSettings(IConfiguration configuration)
    {
        var configuredModems = configuration
            .GetSection("FlConsole:IdentifyModems")
            .GetChildren()
            .Select(section => section.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

        if (configuredModems.Count == 0) throw new InvalidOperationException("Missing required configuration: FlConsole:IdentifyModems");
        return new IdentifyCommandSettings(configuredModems,
            DefaultRsidListenSeconds: RequiredInt(configuration, "FlConsole:Identify:DefaultRsidListenSeconds"),
            DefaultTopCandidates: RequiredInt(configuration, "FlConsole:Identify:DefaultTopCandidates"),
            MinimumQualityToIdentify: RequiredDouble(configuration, "FlConsole:Identify:MinimumQualityToIdentify"),
            FrequencyCarrierSettleDelayMilliseconds: RequiredInt(configuration, "FlConsole:Identify:FrequencyCarrierSettleDelayMilliseconds"),
            RsidSampleIntervalMilliseconds: RequiredInt(configuration, "FlConsole:Identify:RsidSampleIntervalMilliseconds"),
            ModeSettleDelayMilliseconds: RequiredInt(configuration, "FlConsole:Identify:ModeSettleDelayMilliseconds"),
            HeuristicQualitySampleDelayMilliseconds: RequiredInt(configuration, "FlConsole:Identify:HeuristicQualitySampleDelayMilliseconds"));
    }

    private static ConsoleSettings ReadConsoleSettings(IConfiguration configuration) => new(
        RequiredString(configuration, "FlConsole:Console:PromptPrefix"),
        RequiredInt(configuration, "FlConsole:Console:MaxLines"));

    private static CommandMessages ReadCommandMessages(IConfiguration configuration)
    {
        return new CommandMessages(
            RequiredMessage(configuration, "HelpText"), RequiredMessage(configuration, "StartupHint"), RequiredMessage(configuration, "CommandErrorFormat"),
            RequiredMessage(configuration, "UnknownCommandFormat"), RequiredMessage(configuration, "ExecutionErrorFormat"),
            RequiredMessage(configuration, "AdjustUsage"), RequiredMessage(configuration, "AdjustResult"), RequiredMessage(configuration, "SetUsage"), RequiredMessage(configuration, "SetResult"),
            RequiredMessage(configuration, "ScanUsage"), RequiredMessage(configuration, "ScanDone"), RequiredMessage(configuration, "ScanCarrierDebug"), RequiredMessage(configuration, "ScanQualityDebug"), RequiredMessage(configuration, "ScanActivity"),
            RequiredMessage(configuration, "IdentifyUsage"), RequiredMessage(configuration, "IdentifyCurrentModem"), RequiredMessage(configuration, "IdentifySignalFrequency"), RequiredMessage(configuration, "IdentifyCenteredFrequency"), RequiredMessage(configuration, "IdentifyListening"), RequiredMessage(configuration, "IdentifyRsidResult"), RequiredMessage(configuration, "IdentifyNothing"), RequiredMessage(configuration, "IdentifyNoCandidates"), RequiredMessage(configuration, "IdentifyTopCandidates"), RequiredMessage(configuration, "IdentifyCandidate"), RequiredMessage(configuration, "IdentifySelected"), RequiredMessage(configuration, "IdentifyVerboseCandidate"), RequiredMessage(configuration, "MethodUsage"), RequiredMessage(configuration, "MonitorNullValue"), RequiredMessage(configuration, "SetCallUsage"), RequiredMessage(configuration, "TxUsage"), RequiredMessage(configuration, "TxReady"), RequiredMessage(configuration, "TxLocked"), RequiredMessage(configuration, "TxStarted"), RequiredMessage(configuration, "TxDone"));
    }

    private static FrequencyCommandSettings ReadFrequencyCommandSettings(IConfiguration configuration) => new(
        RequiredDouble(configuration, "FlConsole:Frequency:MinCarrierOffsetHz"),
        RequiredDouble(configuration, "FlConsole:Frequency:MaxCarrierOffsetHz"),
        RequiredDouble(configuration, "FlConsole:Frequency:CenterCarrierOffsetHz"),
        RequiredInt(configuration, "FlConsole:Frequency:SettleDelayMilliseconds"));

    private static MonitorCommandSettings ReadMonitorCommandSettings(IConfiguration configuration) => new(
        RequiredInt(configuration, "FlConsole:Monitor:PollIntervalMilliseconds"));

    private static TxCommandSettings ReadTxCommandSettings(IConfiguration configuration) => new(
        RequiredInt(configuration, "FlConsole:Tx:PollIntervalMilliseconds"));

    private static string RequiredMessage(IConfiguration configuration, string name) => RequiredString(configuration, $"FlConsole:Messages:{name}");
    private static string RequiredString(IConfiguration configuration, string key) => configuration[key] ?? throw new InvalidOperationException($"Missing required configuration: {key}");
    private static int RequiredInt(IConfiguration configuration, string key) => int.TryParse(RequiredString(configuration, key), out var value) ? value : throw new InvalidOperationException($"Invalid integer configuration: {key}");
    private static double RequiredDouble(IConfiguration configuration, string key) => double.TryParse(RequiredString(configuration, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : throw new InvalidOperationException($"Invalid number configuration: {key}");
}
