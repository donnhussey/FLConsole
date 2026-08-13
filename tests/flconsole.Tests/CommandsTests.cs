using System.Net;
using System.Net.Http;
using System.Text;
using flconsole.Commands;
using Xunit;

namespace flconsole.Tests;

public class CommandsTests
{
    [Fact]
    public void CommandMetadata_IsConfiguredAsExpected()
    {
        Assert.Equal("help", new HelpCommand().CommandName);
        Assert.False(new HelpCommand().Repeat);
        Assert.Equal(TimeSpan.Zero, new HelpCommand().RepeatInterval);
        Assert.False(new HelpCommand().StopsShell);

        var clearRenderer = new FakePromptAreaRenderer();
        var clearPromptHandler = new ConsolePromptHandler(clearRenderer, new EnterOnlyConsoleInput());
        Assert.Equal("clear", new ClearCommand(clearRenderer, clearPromptHandler).CommandName);
        Assert.False(new ClearCommand(clearRenderer, clearPromptHandler).Repeat);
        Assert.Equal(TimeSpan.Zero, new ClearCommand(clearRenderer, clearPromptHandler).RepeatInterval);
        Assert.False(new ClearCommand(clearRenderer, clearPromptHandler).StopsShell);

        Assert.Equal("quit", new QuitCommand().CommandName);
        Assert.False(new QuitCommand().Repeat);
        Assert.Equal(TimeSpan.Zero, new QuitCommand().RepeatInterval);
        Assert.True(new QuitCommand().StopsShell);

        Assert.Equal("scan", new ScanCommand(CreateClientReturning("ok")).CommandName);
        Assert.False(new ScanCommand(CreateClientReturning("ok")).Repeat);
        Assert.Equal(TimeSpan.Zero, new ScanCommand(CreateClientReturning("ok")).RepeatInterval);
        Assert.False(new ScanCommand(CreateClientReturning("ok")).StopsShell);

        Assert.Equal("adjust", new AdjustCommand(CreateClientReturning("ok")).CommandName);
        Assert.False(new AdjustCommand(CreateClientReturning("ok")).Repeat);
        Assert.Equal(TimeSpan.Zero, new AdjustCommand(CreateClientReturning("ok")).RepeatInterval);
        Assert.False(new AdjustCommand(CreateClientReturning("ok")).StopsShell);

        Assert.Equal("set", new SetCommand(CreateClientReturning("ok")).CommandName);
        Assert.False(new SetCommand(CreateClientReturning("ok")).Repeat);
        Assert.Equal(TimeSpan.Zero, new SetCommand(CreateClientReturning("ok")).RepeatInterval);
        Assert.False(new SetCommand(CreateClientReturning("ok")).StopsShell);

        Assert.Equal("method", new MethodCallCommand(CreateClientReturning("ok")).CommandName);
        Assert.False(new MethodCallCommand(CreateClientReturning("ok")).Repeat);
        Assert.Equal(TimeSpan.Zero, new MethodCallCommand(CreateClientReturning("ok")).RepeatInterval);
        Assert.False(new MethodCallCommand(CreateClientReturning("ok")).StopsShell);

        Assert.Equal("monitor", new MonitorCommand(CreateClientReturning("ok")).CommandName);
        Assert.True(new MonitorCommand(CreateClientReturning("ok")).Repeat);
        Assert.Equal(TimeSpan.FromSeconds(1), new MonitorCommand(CreateClientReturning("ok")).RepeatInterval);
        Assert.False(new MonitorCommand(CreateClientReturning("ok")).StopsShell);

        Assert.Equal("identify", new IdentifyCommand(CreateClientReturning("ok")).CommandName);
        Assert.False(new IdentifyCommand(CreateClientReturning("ok")).Repeat);
        Assert.Equal(TimeSpan.Zero, new IdentifyCommand(CreateClientReturning("ok")).RepeatInterval);
        Assert.False(new IdentifyCommand(CreateClientReturning("ok")).StopsShell);
    }

    [Fact]
    public async Task HelpCommand_ReturnsCommandList()
    {
        var command = new HelpCommand();

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Contains("Commands:", text);
        Assert.Contains("clear", text);
        Assert.Contains("adjust <frequency>", text);
        Assert.Contains("method <method-name>", text);
        Assert.Contains("identify [all] [listen-seconds] [top-candidates] [v]", text);
        Assert.Contains("set <frequency> <rig-mode> <modem-name>", text);
        Assert.Contains("quit", text);
    }

    [Fact]
    public async Task ClearCommand_ClearsOutputBuffer_AndReturnsEmptyStream()
    {
        var renderer = new FakePromptAreaRenderer();
        var promptHandler = new ConsolePromptHandler(renderer, new EnterOnlyConsoleInput());
        var command = new ClearCommand(renderer, promptHandler);

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public async Task AdjustCommand_WritesUsageForInvalidArguments()
    {
        var command = new AdjustCommand(CreateClientReturning("ok"));

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("Usage: adjust <frequency>", text);
    }

    [Fact]
    public async Task AdjustCommand_AdjustsOnlyCarrierWhenFrequencyIsInCurrentBand()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(7072500),
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new AdjustCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(["7074000"]));

        Assert.Equal("Adjusted frequency=7074000, dial=7072500, carrier=1500", text);
        Assert.Equal(3, handler.RequestBodies.Count);
        Assert.Contains("<methodName>rig.take_control</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<methodName>rig.get_frequency</methodName>", handler.RequestBodies[1]);
        Assert.Contains("<methodName>modem.set_carrier</methodName>", handler.RequestBodies[2]);
        Assert.Contains("<double>1500</double>", handler.RequestBodies[2]);
        Assert.DoesNotContain(handler.RequestBodies, body => body.Contains("<methodName>rig.set_frequency</methodName>", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdjustCommand_RecentersWhenFrequencyIsOutsideCurrentBand()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(7072500),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new AdjustCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(["7078000"]));

        Assert.Equal("Adjusted frequency=7078000, dial=7076500, carrier=1500", text);
        Assert.Equal(4, handler.RequestBodies.Count);
        Assert.Contains("<methodName>rig.take_control</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<methodName>rig.get_frequency</methodName>", handler.RequestBodies[1]);
        Assert.Contains("<methodName>rig.set_frequency</methodName>", handler.RequestBodies[2]);
        Assert.Contains("<double>7076500</double>", handler.RequestBodies[2]);
        Assert.Contains("<methodName>modem.set_carrier</methodName>", handler.RequestBodies[3]);
        Assert.Contains("<double>1500</double>", handler.RequestBodies[3]);
    }

    [Fact]
    public async Task AdjustCommand_AdjustsCarrierWhenFrequencyIsInsideRawBand()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(7072500),
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new AdjustCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(["7075450"]));

        Assert.Equal("Adjusted frequency=7075450, dial=7072500, carrier=2950", text);
        Assert.Equal(3, handler.RequestBodies.Count);
        Assert.Contains("<methodName>rig.take_control</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<methodName>rig.get_frequency</methodName>", handler.RequestBodies[1]);
        Assert.Contains("<methodName>modem.set_carrier</methodName>", handler.RequestBodies[2]);
        Assert.Contains("<double>2950</double>", handler.RequestBodies[2]);
        Assert.DoesNotContain(handler.RequestBodies, body => body.Contains("<methodName>rig.set_frequency</methodName>", StringComparison.Ordinal));
    }

    [Fact]
    public async Task IdentifyCommand_WritesUsageForInvalidArguments()
    {
        var command = new IdentifyCommand(CreateClientReturning("ok"));

        var text = await ReadTextAsync(await command.ExecuteAsync(["abc"]));

        Assert.Equal("Usage: identify [all] [listen-seconds] [top-candidates] [v]", text);
    }

    [Fact]
    public async Task IdentifyCommand_UsesRsidBeforeHeuristicSweep()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcBooleanResponse(false),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(14072500),
            CreateXmlRpcDoubleResponse(1500),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(0),
            CreateXmlRpcResponse("Olivia 8-500"),
            CreateXmlRpcDoubleResponse(34.2),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new IdentifyCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(["1", "3"]));

        Assert.Contains("Listening for RSID", text);
        Assert.Contains("RSID identified modem: Olivia 8-500", text);
        Assert.DoesNotContain("heuristic modem sweep", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(handler.RequestBodies, body => body.Contains("<methodName>rig.take_control</methodName>", StringComparison.Ordinal));
        Assert.Contains(handler.RequestBodies, body => body.Contains("<methodName>rig.set_frequency</methodName>", StringComparison.Ordinal));
        Assert.Contains(handler.RequestBodies, body => body.Contains("<double>14072500</double>", StringComparison.Ordinal));
        Assert.Contains(handler.RequestBodies, body => body.Contains("<methodName>modem.set_carrier</methodName>", StringComparison.Ordinal));
        Assert.Contains(handler.RequestBodies, body => body.Contains("<double>1500</double>", StringComparison.Ordinal));
    }

    [Fact]
    public async Task IdentifyCommand_VerboseFlag_EmitsPerCandidateScoresWhenFallingBack()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcBooleanResponse(true),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(14072500),
            CreateXmlRpcDoubleResponse(1500),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(0),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(0),
            CreateXmlRpcDoubleResponse(25),
            CreateXmlRpcArrayResponse(["BPSK31", "Olivia 8-500"]),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(10),
            CreateXmlRpcResponse("plain text"),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(25),
            CreateXmlRpcResponse("plain text"),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new IdentifyCommand(CreateClient(handler), new IdentifyCommandSettings(["BPSK31"]));

        var text = await ReadTextAsync(await command.ExecuteAsync(["all", "1", "2", "v"]));

        Assert.Contains("running heuristic modem sweep", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Verbose candidate: BPSK31", text);
        Assert.Contains("Verbose candidate: Olivia 8-500", text);
        Assert.Contains("Top candidates:", text);
    }

    [Fact]
    public async Task IdentifyCommand_BelowThreshold_SaysNothingToIdentify()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcBooleanResponse(true),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(14072500),
            CreateXmlRpcDoubleResponse(1500),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(0),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(0),
            CreateXmlRpcDoubleResponse(4.9),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new IdentifyCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(["1"]));

        Assert.Contains("nothing to identify", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("heuristic modem sweep", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Top candidates:", text);
    }

    [Fact]
    public async Task IdentifyCommand_UsesConfiguredModemList_WhenAllIsNotSpecified()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcBooleanResponse(true),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(14072500),
            CreateXmlRpcDoubleResponse(1500),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(0),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(0),
            CreateXmlRpcDoubleResponse(25),
            CreateXmlRpcArrayResponse(["BPSK31", "Olivia 8-500"]),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(10),
            CreateXmlRpcResponse("plain text"),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new IdentifyCommand(CreateClient(handler), new IdentifyCommandSettings(["BPSK31"]));

        var text = await ReadTextAsync(await command.ExecuteAsync(["1", "2", "v"]));

        Assert.Contains("Verbose candidate: BPSK31", text);
        Assert.DoesNotContain("Verbose candidate: Olivia 8-500", text);
        Assert.Contains("Selected modem: BPSK31", text);
    }

    [Fact]
    public async Task QuitCommand_ReturnsEmptyOutput()
    {
        var command = new QuitCommand();

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public async Task ScanCommand_WritesUsageForTooManyArguments()
    {
        var command = new ScanCommand(CreateClientReturning("ok"));

        var text = await ReadTextAsync(await command.ExecuteAsync(["1", "2", "debug"]));

        Assert.Equal("Usage: scan [quality-threshold] [debug]", text);
    }

    [Fact]
    public async Task ScanCommand_ReportsActivityAcrossFrequencyRange()
    {
        var responsePayloads = new List<string>
        {
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(7073850),
            CreateXmlRpcIntResponse(1500),
            CreateXmlRpcResponseWithoutParams()
        };

        for (var index = 0; index < 29; index++)
        {
            responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
            responsePayloads.Add(CreateXmlRpcDoubleResponse(index == 1 ? 25.5 : 10));
        }

        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());

        var handler = new QueueResponseHandler(responsePayloads);
        var command = new ScanCommand(CreateClient(handler), new ScanCommandSettings(0));

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.StartsWith(Environment.NewLine, text);
        Assert.Contains("Activity at 7.074.050 Hz (quality=25.5)", text);
        Assert.EndsWith("Done.", text);
        Assert.Equal(66, handler.RequestBodies.Count);
        Assert.Contains("<methodName>rig.take_control</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<methodName>modem.get_name</methodName>", handler.RequestBodies[1]);
        Assert.Contains("<methodName>rig.get_frequency</methodName>", handler.RequestBodies[2]);
        Assert.Contains("<methodName>modem.get_carrier</methodName>", handler.RequestBodies[3]);
        Assert.Contains("<methodName>modem.set_by_name</methodName>", handler.RequestBodies[4]);
        Assert.Contains("<string>CW</string>", handler.RequestBodies[4]);
        Assert.Contains("<methodName>modem.set_carrier</methodName>", handler.RequestBodies[5]);
        Assert.Contains("<int>100</int>", handler.RequestBodies[5]);
        Assert.Contains("<methodName>modem.get_quality</methodName>", handler.RequestBodies[6]);
        Assert.Contains("<methodName>modem.set_carrier</methodName>", handler.RequestBodies[7]);
        Assert.Contains("<int>200</int>", handler.RequestBodies[7]);
        Assert.Contains("<methodName>modem.get_quality</methodName>", handler.RequestBodies[8]);
        Assert.Contains("<methodName>modem.set_by_name</methodName>", handler.RequestBodies[63]);
        Assert.Contains("<string>BPSK31</string>", handler.RequestBodies[63]);
        Assert.Contains("<methodName>rig.set_frequency</methodName>", handler.RequestBodies[64]);
        Assert.Contains("<double>7073850</double>", handler.RequestBodies[64]);
        Assert.Contains("<methodName>modem.set_carrier</methodName>", handler.RequestBodies[65]);
        Assert.Contains("<int>1500</int>", handler.RequestBodies[65]);
    }

    [Fact]
    public async Task ScanCommand_ReturnsEmptyOutputWhenNoActivityIsFound()
    {
        var responsePayloads = new List<string>
        {
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(7073850),
            CreateXmlRpcIntResponse(1500),
            CreateXmlRpcResponseWithoutParams()
        };

        for (var index = 0; index < 29; index++)
        {
            responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
            responsePayloads.Add(CreateXmlRpcDoubleResponse(10));
        }

        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());

        var handler = new QueueResponseHandler(responsePayloads);
        var command = new ScanCommand(CreateClient(handler), new ScanCommandSettings(0));

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal($"{Environment.NewLine}Done.", text);
    }

    [Fact]
    public async Task ScanCommand_UsesOptionalThreshold()
    {
        var responsePayloads = new List<string>
        {
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(7073850),
            CreateXmlRpcIntResponse(1500),
            CreateXmlRpcResponseWithoutParams()
        };

        for (var index = 0; index < 29; index++)
        {
            responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
            responsePayloads.Add(CreateXmlRpcDoubleResponse(index switch
            {
                0 => 4.9,
                1 => 5.1,
                2 => 9,
                _ => 0
            }));
        }

        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());

        var handler = new QueueResponseHandler(responsePayloads);
        var command = new ScanCommand(CreateClient(handler), new ScanCommandSettings(0));

        var text = await ReadTextAsync(await command.ExecuteAsync(["5"]));

        Assert.DoesNotContain("Activity at 1000 Hz", text);
        Assert.Contains("Activity at 7.074.050 Hz (quality=5.1)", text);
        Assert.Contains("Activity at 7.074.150 Hz (quality=9)", text);
        Assert.EndsWith("Done.", text);
    }

    [Fact]
    public async Task ScanCommand_DebugMode_PrintsQualityAtEachStop()
    {
        var responsePayloads = new List<string>
        {
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponse("BPSK31"),
            CreateXmlRpcDoubleResponse(7073850),
            CreateXmlRpcIntResponse(1500),
            CreateXmlRpcResponseWithoutParams()
        };

        for (var index = 0; index < 29; index++)
        {
            responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
            responsePayloads.Add(CreateXmlRpcIntResponse(100 + (index * 100)));
            responsePayloads.Add(CreateXmlRpcDoubleResponse(index switch
            {
                0 => 4.9,
                1 => 5.1,
                _ => 0
            }));
        }

        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());
        responsePayloads.Add(CreateXmlRpcResponseWithoutParams());

        var handler = new QueueResponseHandler(responsePayloads);
        var command = new ScanCommand(CreateClient(handler), new ScanCommandSettings(0));

        var text = await ReadTextAsync(await command.ExecuteAsync(["5", "debug"]));

        Assert.Contains("Carrier requested=100 Hz readback=100 Hz", text);
        Assert.Contains("Carrier requested=200 Hz readback=200 Hz", text);
        Assert.Contains("Quality at 7.073.950 Hz: 4.9", text);
        Assert.Contains("Quality at 7.074.050 Hz: 5.1", text);
        Assert.Contains("Activity at 7.074.050 Hz (quality=5.1)", text);
        Assert.EndsWith("Done.", text);
    }

    [Fact]
    public async Task MethodCallCommand_WritesUsageForMissingMethodName()
    {
        var command = new MethodCallCommand(CreateClientReturning("ok"));

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("Usage: method <method-name> [arg1 arg2 ...]", text);
    }

    [Fact]
    public async Task MethodCallCommand_SendsMethodAndFormatsResponse()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponse("done")
        ]);
        var command = new MethodCallCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(["rig.get_mode", "42"]));

        Assert.Equal("done", text);
        Assert.Single(handler.RequestBodies);
        Assert.Contains("<methodName>rig.get_mode</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<int>42</int>", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task MethodCallCommand_ReportsErrorsFromClient()
    {
        var command = new MethodCallCommand(CreateClient(new ThrowingHandler("boom")));

        var text = await ReadTextAsync(await command.ExecuteAsync(["rig.get_mode"]));

        Assert.Equal("Error: boom", text);
    }

    [Fact]
    public async Task MonitorCommand_FormatsNullPayload()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new MonitorCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("null", text);
        Assert.Single(handler.RequestBodies);
        Assert.Contains("<methodName>rx.get_data</methodName>", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task MonitorCommand_ReportsErrorsFromClient()
    {
        var command = new MonitorCommand(CreateClient(new ThrowingHandler("monitor failed")));

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("Error: monitor failed", text);
    }

    [Fact]
    public async Task MonitorCommand_FormatsStringAndNumericPayloads()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponse("stream-data"),
            CreateXmlRpcIntResponse(42)
        ]);
        var command = new MonitorCommand(CreateClient(handler));

        var first = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));
        var second = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("stream-data", first);
        Assert.Equal("42", second);
    }

    [Fact]
    public async Task MonitorCommand_DecodesBase64PayloadToReadableText()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcBase64Response("Z29pbmc=")
        ]);
        var command = new MonitorCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("going", text);
    }

    [Fact]
    public async Task SetCommand_ImplementsGenericInterfaceAndWritesUsageForIncompleteArguments()
    {
        var command = new SetCommand(CreateClient(new HttpClientHandler()));
        ICommand<IReadOnlyList<string>> genericCommand = command;

        var stream = await genericCommand.ExecuteAsync(["only", "two"]);
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        Assert.Equal("Usage: set <frequency> <rig-mode> <modem-name>", text);
    }

    [Fact]
    public async Task SetCommand_SendsExpectedXmlRpcCallsAndReturnsSummary()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams()
        ]);
        var command = new SetCommand(CreateClient(handler));

        var text = await ReadTextAsync(await command.ExecuteAsync(["14074000", "USB", "Olivia"]));

        Assert.Equal("Set frequency=14074000, rigMode=USB, modem=Olivia", text);
        Assert.Equal(5, handler.RequestBodies.Count);
        Assert.Contains("<methodName>rig.take_control</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<methodName>rig.set_frequency</methodName>", handler.RequestBodies[1]);
        Assert.Contains("<double>14072500</double>", handler.RequestBodies[1]);
        Assert.Contains("<methodName>modem.set_carrier</methodName>", handler.RequestBodies[2]);
        Assert.Contains("<double>1500</double>", handler.RequestBodies[2]);
        Assert.Contains("<methodName>rig.set_mode</methodName>", handler.RequestBodies[3]);
        Assert.Contains("<string>USB</string>", handler.RequestBodies[3]);
        Assert.Contains("<methodName>modem.set_by_name</methodName>", handler.RequestBodies[4]);
        Assert.Contains("<string>Olivia</string>", handler.RequestBodies[4]);
    }

    [Fact]
    public async Task SetCommand_ReportsErrorsFromClient()
    {
        var command = new SetCommand(CreateClient(new ThrowingHandler("set failed")));

        var text = await ReadTextAsync(await command.ExecuteAsync(["14074000", "USB", "Olivia"]));

        Assert.Equal("Error: set failed", text);
    }

    private static FLDigi CreateClient(HttpMessageHandler handler)
    {
        return new FLDigi(
            new XmlRpcConnectionSettings("127.0.0.1", 7362),
            new HttpClient(handler));
    }

    private static FLDigi CreateClientReturning(string value)
    {
        return CreateClient(new QueueResponseHandler([CreateXmlRpcResponse(value)]));
    }

    private static async Task<string> ReadTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static HttpResponseMessage CreateXmlResponse(string responseXml)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseXml, Encoding.UTF8, "text/xml")
        };
    }

    private static string CreateXmlRpcResponse(string value)
    {
        return $"""
<methodResponse>
  <params>
    <param><value><string>{value}</string></value></param>
  </params>
</methodResponse>
""";
    }

    private static string CreateXmlRpcResponseWithoutParams()
    {
        return """
<methodResponse>
  <params />
</methodResponse>
""";
    }

        private static string CreateXmlRpcIntResponse(int value)
        {
                return $"""
<methodResponse>
    <params>
        <param><value><int>{value}</int></value></param>
    </params>
</methodResponse>
""";
        }

        private static string CreateXmlRpcDoubleResponse(double value)
        {
                return $"""
<methodResponse>
    <params>
        <param><value><double>{value.ToString(CultureInfo.InvariantCulture)}</double></value></param>
    </params>
</methodResponse>
""";
        }

        private static string CreateXmlRpcBase64Response(string value)
        {
                return $"""
<methodResponse>
    <params>
        <param><value><base64>{value}</base64></value></param>
    </params>
</methodResponse>
""";
        }

    private static string CreateXmlRpcBooleanResponse(bool value)
    {
        return $"""
<methodResponse>
    <params>
    <param><value><boolean>{(value ? 1 : 0)}</boolean></value></param>
    </params>
</methodResponse>
""";
    }

        private static string CreateXmlRpcArrayResponse(IEnumerable<string> values)
        {
                var entries = string.Join(string.Empty, values.Select(value => $"<value><string>{value}</string></value>"));
                return $"""
<methodResponse>
    <params>
        <param>
            <value>
                <array>
                    <data>{entries}</data>
                </array>
            </value>
        </param>
    </params>
</methodResponse>
""";
        }

    private sealed class QueueResponseHandler(IEnumerable<string> responsePayloads) : HttpMessageHandler
    {
        private readonly Queue<string> _payloads = new(responsePayloads);

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            RequestBodies.Add(body);

            if (_payloads.Count == 0)
            {
                throw new InvalidOperationException("No queued response payload.");
            }

            return CreateXmlResponse(_payloads.Dequeue());
        }
    }

    private sealed class ThrowingHandler(string message) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(message);
        }
    }
}
