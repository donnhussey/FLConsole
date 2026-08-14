
using flconsole.Console;

namespace flconsole;

public interface ICommandResolver
{
    ICommand? Resolve(string commandName);
}
