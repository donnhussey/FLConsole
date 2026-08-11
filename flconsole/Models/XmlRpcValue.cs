using System.Globalization;
using System.Xml.Linq;

namespace flconsole.Models;

public abstract class XmlRpcValue
{
    public abstract object? GetValue();

    public abstract XElement ToXml();

    public static XmlRpcValue? FromXml(XElement valueElement)
    {
        var child = valueElement.Elements().FirstOrDefault();
        if (child is null)
        {
            return new XmlRpcStringValue { Text = valueElement.Value };
        }

        return child.Name.LocalName switch
        {
            "string" => new XmlRpcStringValue { Text = child.Value },
            "int" or "i4" => new XmlRpcIntValue { Text = int.Parse(child.Value, CultureInfo.InvariantCulture) },
            "boolean" => new XmlRpcBooleanValue { Text = child.Value == "1" },
            "double" => new XmlRpcDoubleValue { Text = double.Parse(child.Value, CultureInfo.InvariantCulture) },
            "base64" => new XmlRpcBase64Value { Bytes = Convert.FromBase64String(child.Value) },
            "array" => new XmlRpcArrayValue
            {
                Values = child.Element("data")?.Elements("value")
                    .Select(item => FromXml(item))
                    .Where(item => item is not null)
                    .Cast<XmlRpcValue>()
                    .ToList() ?? []
            },
            "struct" => new XmlRpcStructValue
            {
                Members = child.Elements("member")
                    .Select(member => new XmlRpcMember
                    {
                        Name = member.Element("name")?.Value ?? string.Empty,
                        Value = FromXml(member.Element("value") ?? new XElement("value"))
                    })
                    .ToList()
            },
            _ => new XmlRpcStringValue { Text = child.Value }
        };
    }
}
