namespace flconsole;

public static class ShellCommandParser
{
    public static ShellCommandRequest? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var parts = line.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var commandName = parts[0].ToLowerInvariant();
        var arguments = parts.Skip(1).ToList();
        return new ShellCommandRequest(commandName, arguments);
    }
}