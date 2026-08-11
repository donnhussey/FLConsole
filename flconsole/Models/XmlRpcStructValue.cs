using System.Xml.Linq;

namespace flconsole.Models;

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
