namespace flconsole.Tests;

public class ConsolePromptHandlerInputTests
{
    [Fact]
    public void ReadLineFromPrompt_EnterImmediately_ReturnsEmptyString()
    {
        var renderer = new FakePromptAreaRenderer();
        var input = new QueueConsoleInput([
            Key('\r', ConsoleKey.Enter)
        ]);
        var handler = new ConsolePromptHandler(renderer, input);

        var line = handler.ReadLineFromPrompt();

        Assert.Equal(string.Empty, line);
        Assert.False(handler.IsActive);
        Assert.Equal(string.Empty, renderer.LastPromptText);
        Assert.Equal(0, renderer.LastCursorIndex);
    }

    [Fact]
    public void ReadLineFromPrompt_CtrlD_ReturnsNull()
    {
        var renderer = new FakePromptAreaRenderer();
        var input = new QueueConsoleInput([
            new ConsoleKeyInfo('d', ConsoleKey.D, false, false, true)
        ]);
        var handler = new ConsolePromptHandler(renderer, input);

        var line = handler.ReadLineFromPrompt();

        Assert.Null(line);
        Assert.False(handler.IsActive);
    }

    [Fact]
    public void ReadLineFromPrompt_BackspaceDeleteArrowsAndEscape_AffectBufferAsExpected()
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

        var line = handler.ReadLineFromPrompt();

        Assert.Equal("z", line);
        Assert.Equal("z", handler.CurrentText);
        Assert.Equal(1, handler.CurrentCursorIndex);
        Assert.False(handler.IsActive);
    }

    [Fact]
    public void ReadLineFromPrompt_RightArrow_AtEnd_DoesNotMovePastBuffer()
    {
        var renderer = new FakePromptAreaRenderer();
        var input = new QueueConsoleInput([
            Key('q', ConsoleKey.Q),
            Key('\0', ConsoleKey.RightArrow),
            Key('\0', ConsoleKey.RightArrow),
            Key('\r', ConsoleKey.Enter)
        ]);
        var handler = new ConsolePromptHandler(renderer, input);

        var line = handler.ReadLineFromPrompt();

        Assert.Equal("q", line);
        Assert.Equal(1, handler.CurrentCursorIndex);
    }

    private static ConsoleKeyInfo Key(char character, ConsoleKey key)
    {
        return new ConsoleKeyInfo(character, key, false, false, false);
    }
}