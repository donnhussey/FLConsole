namespace flconsole;

public sealed class TxIdentityState
{
    public string? Callsign { get; private set; }
    public string? Location { get; private set; }
    public bool IsConfigured => Callsign is not null && Location is not null;

    public void Set(string callsign, string location)
    {
        Callsign = callsign;
        Location = location;
    }
}
