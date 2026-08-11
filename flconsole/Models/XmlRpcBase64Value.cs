using System.Xml.Linq;

namespace flconsole.Models;

public sealed class XmlRpcBase64Value : XmlRpcValue
{
    public byte[] Bytes { get; set; } = [];

    public override object? GetValue() => Bytes;

    public override XElement ToXml() => new XElement("base64", Convert.ToBase64String(Bytes));
}
