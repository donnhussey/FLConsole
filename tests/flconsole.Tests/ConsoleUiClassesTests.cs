namespace flconsole.Tests;

public class ConsoleUiClassesTests
{
    [Fact]
    public void ConsoleOutputBuffer_KeepsMostRecentLines()
    {
        var buffer = new ConsoleOutputBuffer(MaxLines: 2);

        buffer.AddLine("first");
        buffer.AddLine("second");
        buffer.AddLine("third");

        var visibleLines = buffer.GetVisibleLines(3);

        Assert.Equal(["second", "third"], visibleLines);
    }

    [Fact]
    public void ConsoleOutputBuffer_AppendText_MergesFragmentsAndSplitsOnNewlines()
    {
        var buffer = new ConsoleOutputBuffer(MaxLines: 10);

        buffer.AppendText("going");
        buffer.AppendText(" region");
        buffer.AppendText(" one\nline two");
        buffer.AppendText(" tail");

        var visibleLines = buffer.GetVisibleLines(10);

        Assert.Equal(["going region one", "line two tail"], visibleLines);
    }

    [Fact]
    public void ConsoleOutputBuffer_GetVisibleRows_WrapsLinesToRequestedWidth()
    {
        var buffer = new ConsoleOutputBuffer(MaxLines: 10);

        buffer.AddLine("abcdefghij");
        buffer.AddLine("klm");

        var visibleRows = buffer.GetVisibleRows(10, 4);

        Assert.Equal(["abcd", "efgh", "ij", "klm"], visibleRows);
    }

    [Fact]
    public void ConsoleOutputBuffer_GetVisibleRows_WrapsOnWhitespaceWhenPossible()
    {
        var buffer = new ConsoleOutputBuffer(MaxLines: 10);

        buffer.AddLine("alpha beta gamma");

        var visibleRows = buffer.GetVisibleRows(10, 10);

        Assert.Equal(["alpha beta", "gamma"], visibleRows);
    }

    [Fact]
    public void ConsoleOutputBuffer_AppendText_IgnoresLoneCarriageReturns()
    {
        var buffer = new ConsoleOutputBuffer(MaxLines: 10);

        buffer.AppendText("c");
        buffer.AppendText("w\r");
        buffer.AppendText("t\re\rs\rt");

        var visibleLines = buffer.GetVisibleLines(10);

        Assert.Equal(["cwtest"], visibleLines);
    }

    [Fact]
    public void PromptEditor_TracksTextAndCursor()
    {
        var editor = new PromptEditor();

        editor.Apply(new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false));
        editor.Apply(new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false));
        editor.Apply(new ConsoleKeyInfo('l', ConsoleKey.L, false, false, false));
        editor.Apply(new ConsoleKeyInfo('l', ConsoleKey.L, false, false, false));
        editor.Apply(new ConsoleKeyInfo('o', ConsoleKey.O, false, false, false));

        Assert.Equal("hello", editor.Text);
        Assert.Equal(5, editor.CursorIndex);
    }

}
