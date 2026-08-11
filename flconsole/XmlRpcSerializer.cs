using System.Globalization;
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

        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        request.ToXDocument().Save(xmlWriter);
        xmlWriter.Flush();
        return stringWriter.ToString().Replace("utf-16", "UTF-8", StringComparison.OrdinalIgnoreCase);
    }

    public static XmlRpcResponse DeserializeResponse(string response)
    {
        var trimmed = response.Trim();
        var payload = trimmed.Length == 0
            ? trimmed
            : trimmed.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase) >= 0
                ? trimmed[trimmed.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase)..]
                : trimmed.IndexOf('<') >= 0
                    ? trimmed[trimmed.IndexOf('<')..]
                    : trimmed;

        var document = XDocument.Parse(payload);
        return XmlRpcResponse.FromXDocument(document);
    }
}
