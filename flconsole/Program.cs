using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace flconsole;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var configuration = BuildConfiguration();
        var serviceCollection = new ServiceCollection()
            .AddFlConsole(configuration);
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var app = serviceProvider.GetRequiredService<FlConsoleApplication>();
        return await app.RunAsync(args, global::System.Console.Out);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("flconsole/appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: false)
            .Build();
    }
}
