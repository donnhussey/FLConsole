using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace flconsole;

public static class ServiceCollectionExtensions
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 7362;
    private const string DefaultPromptPrefix = "flconsole > ";
    private const int DefaultMaxLines = 500;
    private const int DefaultScanSettleDelayMilliseconds = ScanCommandSettings.DefaultSettleDelayMilliseconds;

    public static IServiceCollection AddFlConsole(this IServiceCollection services, IConfiguration configuration)
    {
        var (host, port) = ReadXmlRpcSettings(configuration);
        var connectionSettings = new XmlRpcConnectionSettings(host, port);
        var consoleUiSettings = new ConsoleUiSettings(DefaultPromptPrefix, DefaultMaxLines);
        var scanCommandSettings = ReadScanCommandSettings(configuration);
        var identifyCommandSettings = ReadIdentifyCommandSettings(configuration);

        services.AddSingleton(connectionSettings);
        services.AddSingleton(consoleUiSettings);
        services.AddSingleton(scanCommandSettings);
        services.AddSingleton(identifyCommandSettings);
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<XmlRpcConnectionSettings>();
            return new XmlRpcClient(settings.Host, settings.Port);
        });
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<ConsoleUiSettings>();
            return new ConsoleOutputBuffer(settings.MaxLines);
        });
        services.AddSingleton<IConsoleFacade, SystemConsoleFacade>();
        services.AddSingleton<IConsoleInput, SystemConsoleInput>();
        services.AddSingleton<IRenderer, ConsoleRenderer>();
        services.AddSingleton<ConsolePromptHandler>();
        services.AddSingleton<IPromptReader>(provider => provider.GetRequiredService<ConsolePromptHandler>());
        services.AddSingleton<IPromptState>(provider => provider.GetRequiredService<ConsolePromptHandler>());
        services.AddSingleton<CommandDisplayRunner>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, HelpCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, ClearCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, QuitCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, AdjustCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, SetCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, ScanCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, MonitorCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, IdentifyCommand>();
        services.AddSingleton<ICommand<IReadOnlyList<string>>, MethodCallCommand>();
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
