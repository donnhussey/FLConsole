using System.Net;
using System.Net.Http;
using System.Reflection;
using flconsole.XmlRpc;

namespace flconsole.Tests;

public sealed class XmlRpcApiCoverageTests
{
    [Fact]
    public async Task PublicApiMethodsExecuteThroughInjectedTransport()
    {
        var apiTypes = new[]
        {
            typeof(FLDigi),
            typeof(IoApi),
            typeof(LogApi),
            typeof(MainApi),
            typeof(ModemApi),
            typeof(NavtexApi),
            typeof(RigApi),
            typeof(RxApi),
            typeof(RxtxApi),
            typeof(SpotApi),
            typeof(TextApi),
            typeof(TxApi),
            typeof(WefaxApi)
        };
        var methods = apiTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.ReturnType.IsGenericType
                    && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                .Select(method => (Type: type, Method: method)))
            .ToList();
        var handler = new ApiCoverageHandler(methods.Select(item => CreateResponse(item.Method.ReturnType)));
        var fldigi = new FLDigi(
            new XmlRpcConnectionSettings("127.0.0.1", 7362),
            new HttpClient(handler));

        foreach (var (type, method) in methods)
        {
            var target = type == typeof(FLDigi)
                ? fldigi
                : typeof(FLDigi).GetProperty(GetApiPropertyName(type))?.GetValue(fldigi)
                    ?? throw new InvalidOperationException($"No FLDigi property for {type.Name}.");
            var arguments = method.GetParameters().Select(parameter => CreateArgument(parameter.ParameterType)).ToArray();
            var task = (Task)method.Invoke(target, arguments)!;
            await task;
        }

        Assert.Equal(methods.Count, handler.RequestCount);
        Assert.Equal(methods.Count, handler.MethodNames.Count);
    }

    private static string GetApiPropertyName(Type apiType)
    {
        return apiType.Name[..^3];
    }

    private static object? CreateArgument(Type type)
    {
        if (type == typeof(string))
        {
            return "value";
        }

        if (type == typeof(int))
        {
            return 1;
        }

        if (type == typeof(double))
        {
            return 1.5d;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(byte[]))
        {
            return new byte[] { 1, 2, 3 };
        }

        if (type == typeof(object[]))
        {
            return Array.Empty<object>();
        }

        if (type == typeof(IEnumerable<string>))
        {
            return new[] { "value" };
        }

        throw new InvalidOperationException($"Unsupported API argument type: {type}.");
    }

    private static string CreateResponse(Type taskType)
    {
        var resultType = taskType.GetGenericArguments()[0];
        if (resultType == typeof(string))
        {
            return "<methodResponse><params><param><value><string>value</string></value></param></params></methodResponse>";
        }

        if (resultType == typeof(int))
        {
            return "<methodResponse><params><param><value><int>1</int></value></param></params></methodResponse>";
        }

        if (resultType == typeof(double))
        {
            return "<methodResponse><params><param><value><double>1.5</double></value></param></params></methodResponse>";
        }

        if (resultType == typeof(bool))
        {
            return "<methodResponse><params><param><value><boolean>1</boolean></value></param></params></methodResponse>";
        }

        if (resultType == typeof(object))
        {
            return "<methodResponse><params /></methodResponse>";
        }

        if (resultType == typeof(IReadOnlyList<string>))
        {
            return "<methodResponse><params><param><value><array><data><value><string>value</string></value></data></array></value></param></params></methodResponse>";
        }

        throw new InvalidOperationException($"Unsupported API return type: {resultType}.");
    }

    private sealed class ApiCoverageHandler(IEnumerable<string> payloads) : HttpMessageHandler
    {
        private readonly Queue<string> _payloads = new(payloads);

        public int RequestCount { get; private set; }
        public List<string> MethodNames { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            MethodNames.Add(XDocument.Parse(body).Root?.Element("methodName")?.Value ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payloads.Dequeue(), System.Text.Encoding.UTF8, "text/xml")
            };
        }
    }
}
