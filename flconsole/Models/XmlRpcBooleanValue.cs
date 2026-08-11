using System.Xml.Linq;

namespace flconsole.Models;

public sealed class XmlRpcBooleanValue : XmlRpcValue
{
    public bool Text { get; set; }

    public override object? GetValue() => Text;

    public override XElement ToXml() => new XElement("boolean", Text ? 1 : 0);
}
