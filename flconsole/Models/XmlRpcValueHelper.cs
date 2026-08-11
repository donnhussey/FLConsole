using System.Globalization;
using System.Text.Json;

namespace flconsole.Models;

public static class XmlRpcValueHelper
{
    public static object? ParseParameter(string token)
    {
        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }

        return token;
    }

    public static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        return value switch
        {
            string s => s,
            IEnumerable<object?> enumerable when value is not string => JsonSerializer.Serialize(enumerable),
            _ => JsonSerializer.Serialize(value)
        };
    }
}
