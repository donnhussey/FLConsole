using System.Globalization;

namespace flconsole.Commands;

internal static class CommandArguments
{
    public static bool TryGetPositiveDouble(IReadOnlyList<string> arguments, int index, out double value)
    {
        value = 0;
        return index < arguments.Count
            && double.TryParse(arguments[index], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && value > 0;
    }

    public static bool TryGetFrequency(IReadOnlyList<string> arguments, int index, out double value)
    {
        value = 0;
        if (index >= arguments.Count)
        {
            return false;
        }

        var token = arguments[index];
        if (token.Count(character => character == '.') == 1)
        {
            token = token.Replace(".", string.Empty, StringComparison.Ordinal) + "000";
        }
        else if (token.Contains('.', StringComparison.Ordinal))
        {
            token = token.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return double.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            && value > 0;
    }

    public static bool TryGetPositiveInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result > 0;
    }

    public static bool TryGetNonNegativeDouble(string value, out double result)
    {
        result = 0;
        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result)
            && result >= 0;
    }
}