namespace flconsole;

public interface IShellController
{
    bool IsRunning { get; }
    Task HandleInputAsync(string line);
    Task StopDisplayLoopAsync();
}