namespace flconsole;

public interface IConsoleInput
{
    ConsoleKeyInfo ReadKey(bool intercept);
}