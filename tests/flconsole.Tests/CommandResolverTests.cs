using System.Text;

namespace flconsole.Tests;

public class CommandResolverTests
{
    [Fact]
    public void Resolve_ReturnsNullForEmptyOrWhitespaceName()
    {
        var resolver = new CommandResolver([new ResolverTestCommand("help")]);

        Assert.Null(resolver.Resolve(string.Empty));
        Assert.Null(resolver.Resolve("   "));
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        var command = new ResolverTestCommand("help");
        var resolver = new CommandResolver([command]);

        var resolved = resolver.Resolve("HeLp");

        Assert.Same(command, resolved);
    }

    private sealed class ResolverTestCommand(string name) : ICommand
    {
        public string CommandName { get; } = name;
        public bool Repeat => false;
        public TimeSpan RepeatInterval => TimeSpan.Zero;
        public bool StopsShell => false;

        public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
        {
            await output.WriteAsync("ok", cancellationToken);
        }
    }
}