using System.Xml.Linq;

namespace flconsole.Models;

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
