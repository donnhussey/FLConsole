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

    [Fact]
    public void ReadCommand_UpAndDownNavigateHistoryAndRestoreDraft()
    {
        var renderer = new FakePromptAreaRenderer();
        var input = new QueueConsoleInput([
            Key('h', ConsoleKey.H), Key('i', ConsoleKey.I), Key('\r', ConsoleKey.Enter),
            Key('d', ConsoleKey.D), Key('r', ConsoleKey.R), Key('a', ConsoleKey.A), Key('f', ConsoleKey.F), Key('t', ConsoleKey.T),
            Key('\0', ConsoleKey.UpArrow), Key('\0', ConsoleKey.DownArrow), Key('\r', ConsoleKey.Enter)
        ]);
        var handler = new ConsolePromptHandler(renderer, input);

        Assert.Equal("hi", handler.ReadCommand()?.Name);
        var command = handler.ReadCommand();

        Assert.Equal("draft", command?.Name);
        Assert.Equal("draft", handler.PromptState.Text);
    }

    [Fact]
    public void ReadCommand_StoresAtMostThirtyCommands()
    {
        var renderer = new FakePromptAreaRenderer();
        var keys = new List<ConsoleKeyInfo>();
        for (var index = 0; index < 31; index++)
        {
            var text = $"cmd{index}";
            keys.AddRange(text.Select(character => Key(character, ConsoleKey.A)));
            keys.Add(Key('\r', ConsoleKey.Enter));
        }

        keys.Add(Key('\0', ConsoleKey.UpArrow));
        keys.Add(Key('\r', ConsoleKey.Enter));
        var handler = new ConsolePromptHandler(renderer, new QueueConsoleInput(keys));

        for (var index = 0; index < 31; index++)
        {
            handler.ReadCommand();
        }

        Assert.Equal("cmd30", handler.ReadCommand()?.Name);
    }

    private static ConsoleKeyInfo Key(char character, ConsoleKey key)
    {
        return new ConsoleKeyInfo(character, key, false, false, false);
    }
}