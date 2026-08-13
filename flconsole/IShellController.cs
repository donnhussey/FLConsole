namespace flconsole;

public interface IShellController
{
    bool IsRunning { get; }
    Task HandleCommandAsync(ConsoleCommand request);
    Task StopDisplayLoopAsync();
}