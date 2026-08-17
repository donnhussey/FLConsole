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

    [Fact]
    public void Resolve_DoesNotExposeTxCommandWhenTxIsDisabled()
    {
        var command = new TxResolverTestCommand();
        var resolver = new CommandResolver([command]);

        Assert.Null(resolver.Resolve("tx-test"));
    }

    [Fact]
    public void Resolve_ExposesTxCommandWhenTxIsEnabled()
    {
        var command = new TxResolverTestCommand();
        var resolver = new CommandResolver([command], txEnabled: true);

        Assert.Same(command, resolver.Resolve("tx-test"));
    }

    [Fact]
    public async Task Resolve_ExposesIdentityRequiredCommandOnlyAfterSetCallSucceeds()
    {
        var identityState = new TxIdentityState();
        var command = new IdentityRequiredResolverTestCommand();
        var resolver = new CommandResolver([new SetCallCommand(identityState), command], txEnabled: true, identityState: identityState);

        Assert.Null(resolver.Resolve("identity-required"));

        var output = new CommandTestOutput();
        await new SetCallCommand(identityState).ExecuteAsync(["W1ABC", "FN31"], output);

        Assert.Same(command, resolver.Resolve("identity-required"));
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

    private sealed class TxResolverTestCommand : ITxCommand
    {
        public string CommandName => "tx-test";
        public bool Repeat => false;
        public TimeSpan RepeatInterval => TimeSpan.Zero;
        public bool StopsShell => false;

        public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
        {
            await output.WriteAsync("tx", cancellationToken);
        }
    }

    private sealed class IdentityRequiredResolverTestCommand : ITxIdentityRequiredCommand
    {
        public string CommandName => "identity-required";
        public bool Repeat => false;
        public TimeSpan RepeatInterval => TimeSpan.Zero;
        public bool StopsShell => false;

        public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
        {
            await output.WriteAsync("identity-required", cancellationToken);
        }
    }
}