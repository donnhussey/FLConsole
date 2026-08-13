using System.Globalization;
using System.Xml.Linq;

namespace flconsole.XmlRpc.Models;

internal sealed class XmlRpcValue
{
    private XmlRpcValue(string type, object? data)
    {
        Type = type;
        Data = data;
    }

    private string Type { get; }
    private object? Data { get; }

    public static XmlRpcValue FromObject(object? value) => value switch
    {
        XmlRpcValue node => node,
        null => new XmlRpcValue("string", string.Empty),
        string text => new XmlRpcValue("string", text),
        int number => new XmlRpcValue("int", number),
        bool boolean => new XmlRpcValue("boolean", boolean),
        double number => new XmlRpcValue("double", number),
        byte[] bytes => new XmlRpcValue("base64", bytes),
        Dictionary<string, object?> dictionary => new XmlRpcValue("struct", dictionary.ToDictionary(item => item.Key, item => FromObject(item.Value))),
        IEnumerable<object?> values => new XmlRpcValue("array", values.Select(FromObject).ToList()),
        _ => new XmlRpcValue("string", value.ToString() ?? string.Empty)
    };

    public static XmlRpcValue? FromXml(XElement valueElement)
    {
        var child = valueElement.Elements().FirstOrDefault();
        if (child is null)
        {
            return FromObject(valueElement.Value);
        }

        return child.Name.LocalName switch
        {
            "string" => FromObject(child.Value),
            "int" or "i4" => FromObject(int.Parse(child.Value, CultureInfo.InvariantCulture)),
            "boolean" => FromObject(child.Value == "1"),
            "double" => FromObject(double.Parse(child.Value, CultureInfo.InvariantCulture)),
            "base64" => FromObject(Convert.FromBase64String(child.Value)),
            "array" => new XmlRpcValue("array", child.Element("data")?.Elements("value")
                .Select(FromXml)
                .Where(value => value is not null)
                .Cast<XmlRpcValue>()
                .ToList() ?? []),
            "struct" => new XmlRpcValue("struct", child.Elements("member").ToDictionary(
                member => member.Element("name")?.Value ?? string.Empty,
                member => FromXml(member.Element("value") ?? new XElement("value")))),
            _ => FromObject(child.Value)
        };
    }

    public object? GetValue() => Type switch
    {
        "array" => ((IEnumerable<XmlRpcValue>)Data!).Select(value => value.GetValue()).ToList(),
        "struct" => ((IEnumerable<KeyValuePair<string, XmlRpcValue?>>)Data!).ToDictionary(item => item.Key, item => item.Value?.GetValue()),
        _ => Data
    };

    public XElement ToXml() => Type switch
    {
        "array" => new XElement("array", new XElement("data", ((IEnumerable<XmlRpcValue>)Data!).Select(value => new XElement("value", value.ToXml())))),
        "struct" => new XElement("struct", ((IEnumerable<KeyValuePair<string, XmlRpcValue?>>)Data!).Select(member =>
            new XElement("member", new XElement("name", member.Key), new XElement("value", member.Value?.ToXml())))),
        "boolean" => new XElement(Type, (bool)Data! ? "1" : "0"),
        "base64" => new XElement(Type, Convert.ToBase64String((byte[])Data!)),
        "double" => new XElement(Type, ((double)Data!).ToString(CultureInfo.InvariantCulture)),
        "int" => new XElement(Type, ((int)Data!).ToString(CultureInfo.InvariantCulture)),
        _ => new XElement(Type, Data)
    };
}
