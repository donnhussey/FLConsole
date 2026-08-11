using flconsole.Commands;

namespace flconsole;

public interface ICommandResolver<TRequest>
{
    ICommand<TRequest>? Resolve(string commandName);
}
