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
        output.AppendLine("  method <method-name> [arg1 arg2 ...]  Call an XML-RPC method");
        output.AppendLine("  scan <lower-frequency> <upper-frequency> [step-hz] [quality-threshold]  Continuously scan and report activity");
        output.AppendLine("  set <frequency> <rig-mode> <modem-name>  Set frequency, rig mode, and modem");
        output.AppendLine("  help                                 Show this help text");
        output.AppendLine("  quit                                Exit the shell");
        output.AppendLine();
        output.AppendLine("Examples:");
        output.AppendLine("  method system.listMethods");
        output.AppendLine("  scan 14070000 14088000");
        output.AppendLine("  scan 14070000 14088000 1500 5");
        output.AppendLine("  set 14074000 USB Olivia");

        return Task.FromResult<Stream>(CommandTextStream.Create(output.ToString()));
    }
}
