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

        Assert.Equal("quit", new QuitCommand().CommandName);
        Assert.False(new QuitCommand().Repeat);
        Assert.Equal(TimeSpan.Zero, new QuitCommand().RepeatInterval);
        Assert.True(new QuitCommand().StopsShell);

        Assert.Equal("scan", new ScanCommand(CreateClientReturning("ok")).CommandName);
        Assert.True(new ScanCommand(CreateClientReturning("ok")).Repeat);
        Assert.Equal(TimeSpan.FromSeconds(3), new ScanCommand(CreateClientReturning("ok")).RepeatInterval);
        Assert.False(new ScanCommand(CreateClientReturning("ok")).StopsShell);

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
    }

    [Fact]
    public async Task HelpCommand_ReturnsCommandList()
    {
        var command = new HelpCommand();

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Contains("Commands:", text);
        Assert.Contains("method <method-name>", text);
        Assert.Contains("set <frequency> <rig-mode> <modem-name>", text);
        Assert.Contains("quit", text);
    }

    [Fact]
    public async Task QuitCommand_ReturnsEmptyOutput()
    {
        var command = new QuitCommand();

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public async Task ScanCommand_WritesUsageForMissingBounds()
    {
        var command = new ScanCommand(CreateClientReturning("ok"));

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("Usage: scan <lower-frequency> <upper-frequency> [step-hz] [quality-threshold]", text);
    }

    [Fact]
    public async Task ScanCommand_ReportsActivityAcrossFrequencyRange()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(10),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(25.5),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(10)
        ]);
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(handler));
        var command = new ScanCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(["1000", "7000", "3000"]));

        Assert.Contains("Activity at 4000 Hz (quality=25.5)", text);
        Assert.Equal(7, handler.RequestBodies.Count);
        Assert.Contains("<methodName>rig.take_control</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<methodName>rig.set_frequency</methodName>", handler.RequestBodies[1]);
        Assert.Contains("<double>1000</double>", handler.RequestBodies[1]);
        Assert.Contains("<methodName>modem.get_quality</methodName>", handler.RequestBodies[2]);
        Assert.Contains("<methodName>rig.set_frequency</methodName>", handler.RequestBodies[3]);
        Assert.Contains("<double>4000</double>", handler.RequestBodies[3]);
        Assert.Contains("<methodName>rig.set_frequency</methodName>", handler.RequestBodies[5]);
        Assert.Contains("<double>7000</double>", handler.RequestBodies[5]);
    }

    [Fact]
    public async Task ScanCommand_ReturnsEmptyOutputWhenNoActivityIsFound()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(10),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(10)
        ]);
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(handler));
        var command = new ScanCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(["5000", "1000", "3000"]));

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public async Task ScanCommand_UsesOptionalStepAndThreshold()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(4.9),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(5.1),
            CreateXmlRpcResponseWithoutParams(),
            CreateXmlRpcDoubleResponse(9)
        ]);
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(handler));
        var command = new ScanCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(["1000", "5000", "2000", "5"]));

        Assert.DoesNotContain("Activity at 1000 Hz", text);
        Assert.Contains("Activity at 3000 Hz (quality=5.1)", text);
        Assert.Contains("Activity at 5000 Hz (quality=9)", text);
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
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(handler));
        var command = new MethodCallCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(["rig.get_mode", "42"]));

        Assert.Equal("done", text);
        Assert.Single(handler.RequestBodies);
        Assert.Contains("<methodName>rig.get_mode</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<int>42</int>", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task MethodCallCommand_ReportsErrorsFromClient()
    {
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(new ThrowingHandler("boom")));
        var command = new MethodCallCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(["rig.get_mode"]));

        Assert.Equal("Error: boom", text);
    }

    [Fact]
    public async Task MonitorCommand_FormatsNullPayload()
    {
        var handler = new QueueResponseHandler([
            CreateXmlRpcResponseWithoutParams()
        ]);
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(handler));
        var command = new MonitorCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("null", text);
        Assert.Single(handler.RequestBodies);
        Assert.Contains("<methodName>rx.get_data</methodName>", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task MonitorCommand_ReportsErrorsFromClient()
    {
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(new ThrowingHandler("monitor failed")));
        var command = new MonitorCommand(client);

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
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(handler));
        var command = new MonitorCommand(client);

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
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(handler));
        var command = new MonitorCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(Array.Empty<string>()));

        Assert.Equal("going", text);
    }

    [Fact]
    public async Task SetCommand_ImplementsGenericInterfaceAndWritesUsageForIncompleteArguments()
    {
        var command = new SetCommand(new XmlRpcClient("127.0.0.1", 7362));
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
            CreateXmlRpcResponseWithoutParams()
        ]);
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(handler));
        var command = new SetCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(["14074000", "USB", "Olivia"]));

        Assert.Equal("Set frequency=14074000, rigMode=USB, modem=Olivia", text);
        Assert.Equal(4, handler.RequestBodies.Count);
        Assert.Contains("<methodName>rig.take_control</methodName>", handler.RequestBodies[0]);
        Assert.Contains("<methodName>rig.set_frequency</methodName>", handler.RequestBodies[1]);
        Assert.Contains("<double>14074000</double>", handler.RequestBodies[1]);
        Assert.Contains("<methodName>rig.set_mode</methodName>", handler.RequestBodies[2]);
        Assert.Contains("<string>USB</string>", handler.RequestBodies[2]);
        Assert.Contains("<methodName>modem.set_by_name</methodName>", handler.RequestBodies[3]);
        Assert.Contains("<string>Olivia</string>", handler.RequestBodies[3]);
    }

    [Fact]
    public async Task SetCommand_ReportsErrorsFromClient()
    {
        var client = new XmlRpcClient("127.0.0.1", 7362, new HttpClient(new ThrowingHandler("set failed")));
        var command = new SetCommand(client);

        var text = await ReadTextAsync(await command.ExecuteAsync(["14074000", "USB", "Olivia"]));

        Assert.Equal("Error: set failed", text);
    }

    private static XmlRpcClient CreateClientReturning(string value)
    {
        return new XmlRpcClient("127.0.0.1", 7362, new HttpClient(new QueueResponseHandler([CreateXmlRpcResponse(value)])));
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
