using flconsole.XmlRpc.Models;

namespace flconsole.XmlRpc;

public sealed class FLDigi
{
    private readonly XmlRpcClient _client;

    public FLDigi(XmlRpcConnectionSettings settings, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _client = new XmlRpcClient(settings.Host, settings.Port, httpClient);
        Io = new IoApi(_client);
        Log = new LogApi(_client);
        Main = new MainApi(_client);
        Modem = new ModemApi(_client);
        Navtex = new NavtexApi(_client);
        Rig = new RigApi(_client);
        Rx = new RxApi(_client);
        Rxtx = new RxtxApi(_client);
        Spot = new SpotApi(_client);
        Text = new TextApi(_client);
        Tx = new TxApi(_client);
        Wefax = new WefaxApi(_client);
    }

    public IoApi Io { get; }
    public LogApi Log { get; }
    public MainApi Main { get; }
    public ModemApi Modem { get; }
    public NavtexApi Navtex { get; }
    public RigApi Rig { get; }
    public RxApi Rx { get; }
    public RxtxApi Rxtx { get; }
    public SpotApi Spot { get; }
    public TextApi Text { get; }
    public TxApi Tx { get; }
    public WefaxApi Wefax { get; }

    public Task<object?> InvokeAsync(string methodName, params object?[] parameters) => _client.CallAsync(methodName, parameters).GetValueAsync<object?>(methodName);
    public Task<object?> ConfigDirAsync() => _client.CallAsync("fldigi.config_dir").GetValueAsync<object?>("fldigi.config_dir");
    public Task<object?> ListAsync() => _client.CallAsync("fldigi.list").GetValueAsync<object?>("fldigi.list");
    public Task<object?> NameAsync() => _client.CallAsync("fldigi.name").GetValueAsync<object?>("fldigi.name");
    public Task<object?> NameVersionAsync() => _client.CallAsync("fldigi.name_version").GetValueAsync<object?>("fldigi.name_version");
    public Task<object?> TerminateAsync(int saveFlags) => _client.CallAsync("fldigi.terminate", saveFlags).GetValueAsync<object?>("fldigi.terminate");
    public Task<object?> VersionAsync() => _client.CallAsync("fldigi.version").GetValueAsync<object?>("fldigi.version");
    public Task<object?> VersionStructAsync() => _client.CallAsync("fldigi.version_struct").GetValueAsync<object?>("fldigi.version_struct");
}