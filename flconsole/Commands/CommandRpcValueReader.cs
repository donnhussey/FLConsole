using System.Globalization;

namespace flconsole.Commands;

internal static class CommandRpcValueReader
{
    public static double ReadDoubleOrThrow(object? value, string methodName)
    {
        return value switch
        {
            double doubleValue => doubleValue,
            int intValue => intValue,
            string stringValue when double.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedValue) => parsedValue,
            _ => throw new InvalidOperationException($"{methodName} did not return a numeric value.")
        };
    }

    public static string ReadStringOrThrow(object? value, string methodName)
    {
        return value switch
        {
            string stringValue => stringValue,
            _ => throw new InvalidOperationException($"{methodName} did not return a string value.")
        };
    }

    public static bool ReadBooleanOrThrow(object? value, string methodName)
    {
        return value switch
        {
            bool boolValue => boolValue,
            int intValue => intValue != 0,
            string stringValue when bool.TryParse(stringValue, out var parsedBool) => parsedBool,
            string stringValue when int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt) => parsedInt != 0,
            _ => throw new InvalidOperationException($"{methodName} did not return a boolean value.")
        };
    }

    public static IReadOnlyList<string> ReadStringListOrThrow(object? value, string methodName)
    {
        return value switch
        {
            IEnumerable<object?> values => values
                .Select(item => item?.ToString() ?? string.Empty)
                .ToList(),
            _ => throw new InvalidOperationException($"{methodName} did not return an array of values.")
        };
    }
}