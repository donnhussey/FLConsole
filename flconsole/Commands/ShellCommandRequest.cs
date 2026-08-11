namespace flconsole;

public sealed record ShellCommandRequest(string Name, IReadOnlyList<string> Arguments);