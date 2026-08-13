using System.Xml.Linq;

namespace flconsole.XmlRpc.Models;

internal sealed class XmlRpcRequest
{
    public string MethodName { get; set; } = string.Empty;
    private List<XmlRpcValue?> Values { get; set; } = [];

    public List<object?> Parameters
    {
        get => Values.Select(value => value?.GetValue()).ToList();
        set => Values = value.Select(item => (XmlRpcValue?)XmlRpcValue.FromObject(item)).ToList();
    }

    public XDocument ToXDocument()
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("methodCall",
                new XElement("methodName", MethodName),
                new XElement("params", Values.Select(value => new XElement("param", new XElement("value", value?.ToXml()))))));
    }

    public static XmlRpcRequest FromXDocument(XDocument document)
    {
        var root = document.Root ?? throw new InvalidOperationException("XML-RPC request is missing a root element.");
        return new XmlRpcRequest
        {
            MethodName = root.Element("methodName")?.Value ?? string.Empty,
            Values = root.Element("params")?.Elements("param")
                .Select(parameter => parameter.Element("value") is { } value
                    ? XmlRpcValue.FromXml(value)
                    : null)
                .ToList() ?? []
        };
    }
}

