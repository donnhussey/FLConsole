namespace flconsole.Tests;

public class ConsoleDisplayTests
{
    [Fact]
    public void Clear_CallsConsoleClear()
    {
        var console = new FakeConsoleFacade { WindowWidth = 10, WindowHeight = 4 };
        var renderer = new ConsoleDisplay("fl > ", 10, console);

        renderer.Clear();

        Assert.Equal(1, console.ClearCallCount);
    }

    [Fact]
    public void RenderOutput_ClearsRowsAndWrapsLines()
    {
        var console = new FakeConsoleFacade { WindowWidth = 5, WindowHeight = 4 };
        var renderer = new ConsoleDisplay("fl > ", 10, console);
        var buffer = new ConsoleOutputBuffer(MaxLines: 10);
        buffer.AddLine("abcdef");

        renderer.AppendText("abcdef");

        Assert.Contains("abcde", console.Writes);
        Assert.Contains("f", console.Writes);
        Assert.Contains(new CursorMove(0, 0), console.CursorMoves);
        Assert.Contains(new CursorMove(0, 1), console.CursorMoves);
        Assert.True(console.Writes.Count(text => text == new string(' ', 5)) >= 3);
    }

    [Fact]
    public void RenderInput_WritesPromptAndClampsCursor()
    {
        var console = new FakeConsoleFacade { WindowWidth = 10, WindowHeight = 4 };
        var renderer = new ConsoleDisplay("p> ", 10, console);

        renderer.ShowPrompt("hello", 99);

        Assert.Contains("p> hello", console.Writes);
        Assert.Contains(new CursorMove(0, 3), console.CursorMoves);
        Assert.Contains(new CursorMove(8, 3), console.CursorMoves);
    }

    private sealed class FakeConsoleFacade : IConsoleTerminal
    {
        public int WindowHeight { get; set; }
        public int WindowWidth { get; set; }
        public int ClearCallCount { get; private set; }
        public List<string> Writes { get; } = [];
        public List<CursorMove> CursorMoves { get; } = [];

        public void Clear()
        {
            ClearCallCount++;
        }

        public void SetCursorPosition(int left, int top)
        {
            CursorMoves.Add(new CursorMove(left, top));
        }

        public void Write(string text)
        {
            Writes.Add(text);
        }
    }

    private sealed record CursorMove(int Left, int Top);
}