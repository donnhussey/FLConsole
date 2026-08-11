using System.Text;

namespace flconsole.Tests;

public class CommandResolverTests
{
    [Fact]
    public void Resolve_ReturnsNullForEmptyOrWhitespaceName()
    {
        var resolver = new CommandResolver<IReadOnlyList<string>>([new ResolverTestCommand("help")]);

        Assert.Null(resolver.Resolve(string.Empty));
        Assert.Null(resolver.Resolve("   "));
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        var command = new ResolverTestCommand("help");
        var resolver = new CommandResolver<IReadOnlyList<string>>([command]);

        var resolved = resolver.Resolve("HeLp");

        Assert.Same(command, resolved);
    }

    private sealed class ResolverTestCommand(string name) : ICommand<IReadOnlyList<string>>
    {
        public string CommandName { get; } = name;
        public bool Repeat => false;
        public TimeSpan RepeatInterval => TimeSpan.Zero;
        public bool StopsShell => false;

        public Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
        {
            return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("ok")));
        }
    }
}