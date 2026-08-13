namespace flconsole.Console;

public sealed record ConsoleCommand(string Name, IReadOnlyList<string> Arguments);