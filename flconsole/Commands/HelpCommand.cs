using System.IO;

namespace flconsole.Commands;

public sealed class HelpCommand : ICommand<IReadOnlyList<string>>
{
    public string CommandName => "help";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        var output = new StringBuilder();
        output.AppendLine("Commands:");
        output.AppendLine("  clear                                Clear the console output");
        output.AppendLine("  method <method-name> [arg1 arg2 ...]  Call an XML-RPC method");
        output.AppendLine("  adjust <frequency>  Move to an exact frequency using the current 3 kHz band when possible");
        output.AppendLine("  identify [all] [listen-seconds] [top-candidates] [v]  Center current signal and identify likely modem");
        output.AppendLine("  scan [quality-threshold] [debug]  Scan current 3 kHz segment and report activity");
        output.AppendLine("  set <frequency> <rig-mode> <modem-name>  Set frequency, rig mode, and modem");
        output.AppendLine("  help                                 Show this help text");
        output.AppendLine("  quit                                Exit the shell");
        output.AppendLine();
        output.AppendLine("Examples:");
        output.AppendLine("  clear");
        output.AppendLine("  method system.listMethods");
        output.AppendLine("  adjust 14074000");
        output.AppendLine("  identify 5 5 v");
        output.AppendLine("  identify all 5 5 v");
        output.AppendLine("  scan");
        output.AppendLine("  scan 5");
        output.AppendLine("  scan debug");
        output.AppendLine("  set 14074000 USB Olivia");

        return Task.FromResult<Stream>(CommandTextStream.Create(output.ToString()));
    }
}
