using System.Xml.Linq;

namespace flconsole.XmlRpc.Models;

internal sealed class XmlRpcResponse
{
    public List<object?> Parameters { get; set; } = [];

    public object? Value
    {
        get => Parameters.FirstOrDefault();
        set => Parameters = value is null ? [] : [value];
    }

    public T GetValue<T>(string methodName)
    {
        var value = Value;
        if (value is null)
        {
            if (!typeof(T).IsValueType)
            {
                return default!;
            }

            throw new InvalidOperationException($"{methodName} returned no value.");
        }

        return value switch
        {
            T typedValue => typedValue,
            string text when typeof(T) == typeof(double) && double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var number) => (T)(object)number,
            int integer when typeof(T) == typeof(double) => (T)(object)(double)integer,
            int integer when typeof(T) == typeof(bool) => (T)(object)(integer != 0),
            string text when typeof(T) == typeof(bool) && bool.TryParse(text, out var boolean) => (T)(object)boolean,
            IEnumerable<object?> values when typeof(T) == typeof(IReadOnlyList<string>) => (T)(object)values.Select(item => item?.ToString() ?? string.Empty).ToList(),
            _ => throw new InvalidOperationException($"{methodName} did not return {typeof(T).Name}.")
        };
    }

    public static async Task<T> GetValueAsync<T>(Task<XmlRpcResponse> responseTask, string methodName)
    {
        var response = await responseTask;
        return response.GetValue<T>(methodName);
    }

    public XDocument ToXDocument()
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("methodResponse",
                new XElement("params", Parameters.Select(value => new XElement("param", new XElement("value", XmlRpcValue.FromObject(value).ToXml()))))));
    }

    public static XmlRpcResponse FromXDocument(XDocument document)
    {
        var root = document.Root ?? throw new InvalidOperationException("XML-RPC response is missing a root element.");
        return new XmlRpcResponse
        {
            Parameters = root.Element("params")?.Elements("param")
                .Select(parameter => parameter.Element("value") is { } value
                    ? XmlRpcValue.FromXml(value)?.GetValue()
                    : null)
                .ToList() ?? []
        };
    }
}

