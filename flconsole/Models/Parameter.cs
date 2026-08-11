using System.Xml.Linq;

namespace flconsole.Models;

public class Parameter
{
    public XmlRpcValue? Value { get; set; }

    public XElement ToXml()
    {
        return new XElement("param", new XElement("value", Value?.ToXml()));
    }

    public static Parameter FromXml(XElement element)
    {
        var valueElement = element.Element("value");
        return new Parameter
        {
            Value = valueElement is null ? null : XmlRpcValue.FromXml(valueElement)
        };
    }
}
