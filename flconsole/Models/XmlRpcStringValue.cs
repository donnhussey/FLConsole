using System.Xml.Linq;

namespace flconsole.Models;

public sealed class XmlRpcStringValue : XmlRpcValue
{
    public string Text { get; set; } = string.Empty;

    public override object? GetValue() => Text;

    public override XElement ToXml() => new XElement("string", Text);
}
