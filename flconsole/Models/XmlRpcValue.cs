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

public sealed class XmlRpcStringValue : XmlRpcValue
{
    public string Text { get; set; } = string.Empty;

    public override object? GetValue() => Text;

    public override XElement ToXml() => new XElement("string", Text);
}

public sealed class XmlRpcIntValue : XmlRpcValue
{
    public int Text { get; set; }

    public override object? GetValue() => Text;

    public override XElement ToXml() => new XElement("int", Text.ToString(CultureInfo.InvariantCulture));
}

public sealed class XmlRpcBooleanValue : XmlRpcValue
{
    public bool Text { get; set; }

    public override object? GetValue() => Text;

    public override XElement ToXml() => new XElement("boolean", Text ? 1 : 0);
}

public sealed class XmlRpcDoubleValue : XmlRpcValue
{
    public double Text { get; set; }

    public override object? GetValue() => Text;

    public override XElement ToXml() => new XElement("double", Text.ToString(CultureInfo.InvariantCulture));
}

public sealed class XmlRpcArrayValue : XmlRpcValue
{
    public List<XmlRpcValue> Values { get; set; } = [];

    public override object? GetValue() => Values.Select(value => value.GetValue()).ToList();

    public override XElement ToXml()
    {
        return new XElement("array",
            new XElement("data",
                Values.Select(value => new XElement("value", value.ToXml()))));
    }
}

public sealed class XmlRpcStructValue : XmlRpcValue
{
    public List<XmlRpcMember> Members { get; set; } = [];

    public override object? GetValue() => Members.ToDictionary(member => member.Name, member => member.Value?.GetValue());

    public override XElement ToXml()
    {
        return new XElement("struct",
            Members.Select(member =>
                new XElement("member",
                    new XElement("name", member.Name),
                    new XElement("value", member.Value?.ToXml()))));
    }
}

public sealed class XmlRpcMember
{
    public string Name { get; set; } = string.Empty;

    public XmlRpcValue? Value { get; set; }
}
