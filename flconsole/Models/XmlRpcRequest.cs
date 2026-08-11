using System.Xml.Linq;

namespace flconsole.Models;

public sealed class XmlRpcRequest
{
    public XmlRpcRequest()
    {
        MethodCall = new MethodCall();
    }

    public MethodCall MethodCall { get; set; }

    public string MethodName
    {
        get => MethodCall.MethodName;
        set => MethodCall.MethodName = value;
    }

    public List<object?> Parameters
    {
        get => MethodCall.Parameters.Select(parameter => parameter.Value?.GetValue()).ToList();
        set => MethodCall.Parameters = value.Select(item => new Parameter { Value = CreateValueNode(item) }).ToList();
    }

    internal static XmlRpcValue? CreateValueNode(object? value)
    {
        return value switch
        {
            null => new XmlRpcStringValue { Text = string.Empty },
            string s => new XmlRpcStringValue { Text = s },
            int i => new XmlRpcIntValue { Text = i },
            bool b => new XmlRpcBooleanValue { Text = b },
            double d => new XmlRpcDoubleValue { Text = d },
            IEnumerable<object?> enumerable => new XmlRpcArrayValue
            {
                Values = enumerable.Select(CreateValueNode).Where(item => item is not null).Cast<XmlRpcValue>().ToList()
            },
            Dictionary<string, object?> dictionary => new XmlRpcStructValue
            {
                Members = dictionary.Select(kvp => new XmlRpcMember
                {
                    Name = kvp.Key,
                    Value = CreateValueNode(kvp.Value)
                }).ToList()
            },
            XmlRpcValue xmlRpcValue => xmlRpcValue,
            _ => new XmlRpcStringValue { Text = value.ToString() ?? string.Empty }
        };
    }

    public XDocument ToXDocument()
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("methodCall",
                new XElement("methodName", MethodCall.MethodName),
                new XElement("params", MethodCall.Parameters.Select(parameter => parameter.ToXml()))));
    }

    public static XmlRpcRequest FromXDocument(XDocument document)
    {
        var root = document.Root ?? throw new InvalidOperationException("XML-RPC request is missing a root element.");
        var methodName = root.Element("methodName")?.Value ?? string.Empty;
        var parameters = root.Element("params")?
            .Elements("param")
            .Select(Parameter.FromXml)
            .ToList() ?? [];

        return new XmlRpcRequest
        {
            MethodCall = new MethodCall
            {
                MethodName = methodName,
                Parameters = parameters
            }
        };
    }
}

