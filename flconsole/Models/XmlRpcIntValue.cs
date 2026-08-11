using System.Globalization;
using System.Xml.Linq;

namespace flconsole.Models;

public sealed class XmlRpcIntValue : XmlRpcValue
{
    public int Text { get; set; }

    public override object? GetValue() => Text;

    public override XElement ToXml() => new XElement("int", Text.ToString(CultureInfo.InvariantCulture));
}
