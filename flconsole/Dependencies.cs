using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace flconsole;

public static class ServiceCollectionExtensions
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 7362;
    private const string DefaultPromptPrefix = "flconsole > ";
    private const string DefaultUnknownCommandFormat = "Unknown command: {0}. Type 'help' for commands.";
    private const string DefaultExecutionErrorFormat = "Error: {0}";
    private const int DefaultMaxLines = 500;
    private const int DefaultScanSettleDelayMilliseconds = ScanCommandSettings.DefaultSettleDelayMilliseconds;

    public static IServiceCollection AddFlConsole(this IServiceCollection services, IConfiguration configuration)
    {
        var (host, port) = ReadXmlRpcSettings(configuration);
        var connectionSettings = new XmlRpcConnectionSettings(host, port);
        var shellMessages = new ShellMessages(DefaultUnknownCommandFormat, DefaultExecutionErrorFormat);
        var scanCommandSettings = ReadScanCommandSettings(configuration);
        var identifyCommandSettings = ReadIdentifyCommandSettings(configuration);

        services.AddSingleton(connectionSettings);
        services.AddSingleton(shellMessages);
        services.AddSingleton(scanCommandSettings);
        services.AddSingleton(identifyCommandSettings);
        services.AddSingleton<HttpClient>();
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<XmlRpcConnectionSettings>();
            var httpClient = provider.GetRequiredService<HttpClient>();
            return new FLDigi(settings, httpClient);
        });
        services.AddSingleton<IConsole>(_ => ConsoleFactory.Create(DefaultPromptPrefix, DefaultMaxLines));
        services.AddSingleton(provider => provider.GetRequiredService<IConsole>().Display);
        services.AddSingleton(provider => provider.GetRequiredService<IConsole>().CommandSource);
        services.AddSingleton<CommandDisplayRunner>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, HelpCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, ClearCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, QuitCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, AdjustCommand>(provider => new AdjustCommand(provider.GetRequiredService<FLDigi>()));
        services.AddSingleton<ICommand<IReadOnlyList<string>>, SetCommand>(provider => new SetCommand(provider.GetRequiredService<FLDigi>()));
        services.AddSingleton<ICommand<IReadOnlyList<string>>, ScanCommand>(provider => new ScanCommand(provider.GetRequiredService<FLDigi>()));
        services.AddSingleton<ICommand<IReadOnlyList<string>>, MonitorCommand>(provider => new MonitorCommand(provider.GetRequiredService<FLDigi>()));
        services.AddSingleton<ICommand<IReadOnlyList<string>>, IdentifyCommand>(provider => new IdentifyCommand(provider.GetRequiredService<FLDigi>()));
        services.AddSingleton<ICommand<IReadOnlyList<string>>, MethodCallCommand>(provider => new MethodCallCommand(provider.GetRequiredService<FLDigi>()));
        services.AddSingleton<ICommandResolver<IReadOnlyList<string>>, CommandResolver<IReadOnlyList<string>>>();
        services.AddSingleton<FlConsoleShellController>();
        services.AddSingleton<IShellController>(provider => provider.GetRequiredService<FlConsoleShellController>());
        services.AddSingleton<FlConsoleApplication>();

        return services;
    }

    private static (string Host, int Port) ReadXmlRpcSettings(IConfiguration configuration)
    {
        var host = configuration["FlConsole:Host"];
        var portText = configuration["FlConsole:Port"];

        return (
            string.IsNullOrWhiteSpace(host) ? DefaultHost : host,
            int.TryParse(portText, out var port) ? port : DefaultPort);
    }

    private static ScanCommandSettings ReadScanCommandSettings(IConfiguration configuration)
    {
        var settleDelayText = configuration["FlConsole:ScanSettleDelayMilliseconds"];
        var settleDelayMilliseconds = int.TryParse(settleDelayText, out var parsedDelay) && parsedDelay >= 0
            ? parsedDelay
            : DefaultScanSettleDelayMilliseconds;

        return new ScanCommandSettings(settleDelayMilliseconds);
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

        return new IdentifyCommandSettings(configuredModems);
    }
}
