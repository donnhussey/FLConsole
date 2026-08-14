
using flconsole.Console;

namespace flconsole;

public sealed class CommandResolver(IEnumerable<ICommand> commands) : ICommandResolver
{
    private readonly IReadOnlyDictionary<string, ICommand> _commands = commands
        .ToDictionary(command => command.CommandName, StringComparer.OrdinalIgnoreCase);

    public ICommand? Resolve(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return null;
        }

        return _commands.GetValueOrDefault(commandName);
    }
}
