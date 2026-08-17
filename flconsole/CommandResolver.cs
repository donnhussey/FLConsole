
using flconsole.Console;

namespace flconsole;

public sealed class CommandResolver(IEnumerable<ICommand> commands, bool txEnabled = false, TxIdentityState? identityState = null) : ICommandResolver
{
    private readonly IReadOnlyDictionary<string, ICommand> _commands = commands
        .Where(command => txEnabled || command is not ITxCommand)
        .ToDictionary(command => command.CommandName, StringComparer.OrdinalIgnoreCase);
    private readonly TxIdentityState _identityState = identityState ?? new();

    public ICommand? Resolve(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return null;
        }

        var command = _commands.GetValueOrDefault(commandName);
        return command is ITxIdentityRequiredCommand && !_identityState.IsConfigured ? null : command;
    }
}
