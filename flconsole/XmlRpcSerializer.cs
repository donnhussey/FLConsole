using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using flconsole.Models;

namespace flconsole;

public static class XmlRpcSerializer
{
    public static string SerializeRequest(XmlRpcRequest request)
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false),
            Indent = false,
            NewLineHandling = NewLineHandling.None
        };

        using var stringWriter = new Utf8StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        request.ToXDocument().Save(xmlWriter);
        xmlWriter.Flush();
        return stringWriter.ToString();
    }

    public static XmlRpcResponse DeserializeResponse(string response)
    {
        var payload = ExtractPayload(response);

        var document = XDocument.Parse(payload);
        return XmlRpcResponse.FromXDocument(document);
    }

    private static string ExtractPayload(string response)
    {
        var trimmed = response.Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        var xmlDeclarationIndex = trimmed.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
        if (xmlDeclarationIndex >= 0)
        {
            return trimmed[xmlDeclarationIndex..];
        }

        var rootElementIndex = trimmed.IndexOf('<');
        return rootElementIndex >= 0 ? trimmed[rootElementIndex..] : trimmed;
    }
}
