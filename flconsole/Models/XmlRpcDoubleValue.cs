using System.Globalization;
using System.Xml.Linq;

namespace flconsole.Models;

public sealed class XmlRpcDoubleValue : XmlRpcValue
{
    public double Text { get; set; }

    public override object? GetValue() => Text;

    public override XElement ToXml() => new XElement("double", Text.ToString(CultureInfo.InvariantCulture));
}
