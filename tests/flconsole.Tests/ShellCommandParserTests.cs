namespace flconsole.Tests;

public class ShellCommandParserTests
{
    [Fact]
    public void Parse_ReturnsNullForEmptyInput()
    {
        Assert.Null(ShellCommandParser.Parse(string.Empty));
        Assert.Null(ShellCommandParser.Parse("   "));
    }

    [Fact]
    public void Parse_ReturnsCommandNameLowerCasedAndArguments()
    {
        var request = ShellCommandParser.Parse("  MeThOd   rig.get_mode   42  USB  ");

        Assert.NotNull(request);
        Assert.Equal("method", request!.Name);
        Assert.Equal(["rig.get_mode", "42", "USB"], request.Arguments);
    }
}