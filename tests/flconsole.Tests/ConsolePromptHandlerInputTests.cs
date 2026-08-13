namespace flconsole.Tests;

public class ConsolePromptHandlerInputTests
{
    [Fact]
    public void ReadCommand_EnterImmediately_ReturnsNull()
    {
        var renderer = new FakePromptAreaRenderer();
        var input = new QueueConsoleInput([
            Key('\r', ConsoleKey.Enter)
        ]);
        var handler = new ConsolePromptHandler(renderer, input);

        var command = handler.ReadCommand();

        Assert.Null(command);
        Assert.Equal(string.Empty, renderer.LastPromptText);
        Assert.Equal(0, renderer.LastCursorIndex);
    }

    [Fact]
    public void ReadCommand_CtrlD_ReturnsNull()
    {
        var renderer = new FakePromptAreaRenderer();
        var input = new QueueConsoleInput([
            new ConsoleKeyInfo('d', ConsoleKey.D, false, false, true)
        ]);
        var handler = new ConsolePromptHandler(renderer, input);

        var command = handler.ReadCommand();

        Assert.Null(command);
    }

    [Fact]
    public void ReadCommand_BackspaceDeleteArrowsAndEscape_AffectBufferAsExpected()
    {
        var renderer = new FakePromptAreaRenderer();
        var input = new QueueConsoleInput([
            Key('a', ConsoleKey.A),
            Key('b', ConsoleKey.B),
            Key('c', ConsoleKey.C),
            Key('\0', ConsoleKey.LeftArrow),
            Key('\0', ConsoleKey.Backspace),
            Key('x', ConsoleKey.X),
            Key('\0', ConsoleKey.LeftArrow),
            Key('\0', ConsoleKey.Delete),
            Key('\0', ConsoleKey.Escape),
            Key('z', ConsoleKey.Z),
            Key('\r', ConsoleKey.Enter)
        ]);
        var handler = new ConsolePromptHandler(renderer, input);

        var command = handler.ReadCommand();

        Assert.Equal("z", command?.Name);
        Assert.Equal("z", handler.PromptState.Text);
        Assert.Equal(1, handler.PromptState.CursorIndex);
    }

    [Fact]
    public void ReadCommand_RightArrow_AtEnd_DoesNotMovePastBuffer()
    {
        var renderer = new FakePromptAreaRenderer();
        var input = new QueueConsoleInput([
            Key('q', ConsoleKey.Q),
            Key('\0', ConsoleKey.RightArrow),
            Key('\0', ConsoleKey.RightArrow),
            Key('\r', ConsoleKey.Enter)
        ]);
        var handler = new ConsolePromptHandler(renderer, input);

        var command = handler.ReadCommand();

        Assert.Equal("q", command?.Name);
        Assert.Equal(1, handler.PromptState.CursorIndex);
    }

    private static ConsoleKeyInfo Key(char character, ConsoleKey key)
    {
        return new ConsoleKeyInfo(character, key, false, false, false);
    }
}