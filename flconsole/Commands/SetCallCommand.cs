using System.Text.RegularExpressions;

namespace flconsole.Commands;

public sealed class SetCallCommand(TxIdentityState? identityState = null, CommandMessages? messages = null) : ITxCommand
{
    private static readonly Regex UsCallsignPattern = new(
        "^(?:[KNW][0-9]|A[A-L][0-9]|[KNW][A-Z][0-9])[A-Z]{1,3}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly CommandMessages _messages = messages ?? CommandMessages.Defaults;
    private readonly TxIdentityState _identityState = identityState ?? new();

    public string CommandName => "setcall";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task ExecuteAsync(IReadOnlyList<string> arguments, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        if (arguments.Count != 2 || !IsUsCallsign(arguments[0]) || !IsUsMaidenheadLocator(arguments[1]))
        {
            await output.WriteAsync(_messages.SetCallUsage, cancellationToken);
            return;
        }

        _identityState.Set(arguments[0].ToUpperInvariant(), arguments[1].ToUpperInvariant());
        await output.WriteAsync(string.Format(_messages.TxReady, arguments[0].ToUpperInvariant(), arguments[1].ToUpperInvariant()), cancellationToken);
    }

    internal static bool IsUsCallsign(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && UsCallsignPattern.IsMatch(value);
    }

    internal static bool IsUsMaidenheadLocator(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || (value.Length != 4 && value.Length != 6))
        {
            return false;
        }

        value = value.ToUpperInvariant();
        if (value[0] is < 'A' or > 'R' || value[1] is < 'A' or > 'R' || !char.IsDigit(value[2]) || !char.IsDigit(value[3]) ||
            (value.Length == 6 && (value[4] is < 'A' or > 'X' || value[5] is < 'A' or > 'X')))
        {
            return false;
        }

        var longitude = -180d + (value[0] - 'A') * 20 + (value[2] - '0') * 2;
        var latitude = -90d + (value[1] - 'A') * 10 + (value[3] - '0');
        if (value.Length == 6)
        {
            longitude += (value[4] - 'A') * (2d / 24);
            latitude += (value[5] - 'A') * (1d / 24);
        }

        longitude += value.Length == 4 ? 1 : 1d / 24;
        latitude += value.Length == 4 ? 0.5 : 1d / 48;
        return IsUsTerritory(longitude, latitude);
    }

    private static bool IsUsTerritory(double longitude, double latitude)
    {
        return IsWithin(longitude, latitude, -124.85, -66.88, 24.39, 49.39) ||
               IsWithin(longitude, latitude, -170.0, -129.0, 51.2, 71.5) ||
               IsWithin(longitude, latitude, -160.4, -154.7, 18.8, 22.3) ||
               IsWithin(longitude, latitude, -67.3, -65.5, 17.8, 18.6) ||
               IsWithin(longitude, latitude, -65.1, -64.4, 17.6, 18.5) ||
               IsWithin(longitude, latitude, 144.5, 146.2, 13.1, 14.3) ||
               IsWithin(longitude, latitude, 144.0, 146.2, 14.0, 20.7) ||
               IsWithin(longitude, latitude, -171.2, -169.2, -14.5, -11.0);
    }

    private static bool IsWithin(double longitude, double latitude, double minimumLongitude, double maximumLongitude, double minimumLatitude, double maximumLatitude)
    {
        return longitude >= minimumLongitude && longitude <= maximumLongitude && latitude >= minimumLatitude && latitude <= maximumLatitude;
    }
}
