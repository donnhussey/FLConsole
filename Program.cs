using System.Globalization;
using flconsole.Models;

namespace flconsole;

internal static class Program
{
    private static XmlRpcClient Client { get; set; } = null!;

    private static async Task<int> Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        var host = "127.0.0.1";
        var port = 7362;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--host":
                    host = GetRequiredValue(args, ref index, "--host");
                    break;
                case "--port":
                    port = int.Parse(GetRequiredValue(args, ref index, "--port"), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        Client = new XmlRpcClient(host, port);

        Console.WriteLine($"FLDigi XML-RPC shell (host={host}, port={port})");
        Console.WriteLine("Type 'help' for commands, or 'quit' to exit.");

        while (true)
        {
            Console.Write("flconsole > ");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

        var parts = line.Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
            
            if (parts.Count == 0)
            {
                continue;
            }

            var commandName = parts[0].ToLowerInvariant();
            var arguments = parts.Skip(1).ToList();
            var exit = false;

            switch (commandName)
            {
                case "quit":
                case "exit":
                    exit = true;
                    break;
                case "help":
                    await HandleHelpAsync();
                    break;
                case "set":
                    await HandleSetAsync(arguments);
                    break;
                case "monitor":
                    await HandleMonitorAsync();
                    break;
                case "scan":
                    await HandleScanAsync();
                    break;
                default:
                    await HandleMethodCallAsync(commandName, arguments);
                    break;
            }

            if(exit) break;
        }

        return 0;
    }

    private static async Task HandleScanAsync()
    {
        throw new NotImplementedException();
    }

    private static async Task HandleMonitorAsync()
    {
        throw new NotImplementedException();
    }

    private static Task HandleHelpAsync()
    {
        PrintHelp();
        return Task.CompletedTask;
    }

    private static async Task HandleSetAsync(IReadOnlyList<string> arguments)
    {
        if (arguments.Count < 3)
        {
            Console.WriteLine("Usage: set <frequency> <rig-mode> <modem-name>");
            return;
        }

        try
        {
            var frequency = arguments[0];
            var rigMode = arguments[1];
            var modemName = arguments[2];

            await Client.SendAsync(new XmlRpcRequest
            {
                MethodName = "rig.take_control",
                Parameters = []
            });
            await Client.SendAsync(new XmlRpcRequest
            {
                MethodName = "rig.set_frequency",
                Parameters = [double.Parse(frequency, CultureInfo.InvariantCulture)]
            });
            await Client.SendAsync(new XmlRpcRequest
            {
                MethodName = "rig.set_mode",
                Parameters = [rigMode]
            });
            await Client.SendAsync(new XmlRpcRequest
            {
                MethodName = "modem.set_by_name",
                Parameters = [modemName]
            });

            Console.WriteLine($"Set frequency={frequency}, rigMode={rigMode}, modem={modemName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task HandleMethodCallAsync(string methodName, IReadOnlyList<string> arguments)
    {
        var parameters = arguments.Select(XmlRpcValueHelper.ParseParameter).ToList();
        try
        {
            var request = new XmlRpcRequest
            {
                MethodName = methodName,
                Parameters = parameters
            };
            var response = await Client.SendAsync(request);

            Console.WriteLine(XmlRpcValueHelper.FormatValue(response.Value));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static string GetRequiredValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {optionName}.");
        }

        index++;
        return args[index];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  flconsole [--host <host>] [--port <port>]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  flconsole");
        Console.WriteLine("  flconsole --host 127.0.0.1 --port 7362");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  <method-name> [arg1 arg2 ...]  Call an XML-RPC method");
        Console.WriteLine("  set <frequency> <rig-mode> <modem-name>  Set frequency, rig mode, and modem");
        Console.WriteLine("  help                                 Show this help text");
        Console.WriteLine("  quit                                Exit the shell");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  system.listMethods");
        Console.WriteLine("  set 14074000 USB Olivia");
    }
}
