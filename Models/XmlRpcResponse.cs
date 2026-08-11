using System.Xml.Linq;

namespace flconsole.Models;

public sealed class XmlRpcResponse
{
    public XmlRpcResponse()
    {
        MethodResponse = new MethodResponse();
    }

    public MethodResponse MethodResponse { get; set; }

    public object? Value
    {
        get => MethodResponse.Parameters.FirstOrDefault()?.Value?.GetValue();
        set
        {
            MethodResponse.Parameters.Clear();
            if (value is not null)
            {
                MethodResponse.Parameters.Add(new Parameter { Value = XmlRpcRequest.CreateValueNode(value) });
            }
        }
    }

    public XDocument ToXDocument()
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("methodResponse",
                new XElement("params", MethodResponse.Parameters.Select(parameter => parameter.ToXml()))));
    }

    public static XmlRpcResponse FromXDocument(XDocument document)
    {
        var root = document.Root ?? throw new InvalidOperationException("XML-RPC response is missing a root element.");
        var parameters = root.Element("params")?
            .Elements("param")
            .Select(Parameter.FromXml)
            .ToList() ?? [];

        return new XmlRpcResponse
        {
            MethodResponse = new MethodResponse
            {
                Parameters = parameters
            }
        };
    }
}

