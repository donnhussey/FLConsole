using flconsole.Commands;

namespace flconsole;

public sealed class CommandResolver<TRequest>(IEnumerable<ICommand<TRequest>> commands) : ICommandResolver<TRequest>
{
    private readonly IReadOnlyDictionary<string, ICommand<TRequest>> _commands = commands
        .ToDictionary(command => command.CommandName, StringComparer.OrdinalIgnoreCase);

    public ICommand<TRequest>? Resolve(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return null;
        }

        return _commands.GetValueOrDefault(commandName);
    }
}
