namespace flconsole.XmlRpc;

public sealed class ModemApi : Api
{
    internal ModemApi(XmlRpcClient client) : base(client) { }

    public Task<int> GetAfcSearchRangeAsync() => Client.CallAsync("modem.get_afc_search_range").GetValueAsync<int>("modem.get_afc_search_range");
    public Task<int> GetBandwidthAsync() => Client.CallAsync("modem.get_bandwidth").GetValueAsync<int>("modem.get_bandwidth");
    public Task<string> GetNameAsync() => Client.CallAsync("modem.get_name").GetValueAsync<string>("modem.get_name");
    public Task<IReadOnlyList<string>> GetNamesAsync() => Client.CallAsync("modem.get_names").GetValueAsync<IReadOnlyList<string>>("modem.get_names");
    public Task<int> GetIdAsync() => Client.CallAsync("modem.get_id").GetValueAsync<int>("modem.get_id");
    public Task<int> GetMaxIdAsync() => Client.CallAsync("modem.get_max_id").GetValueAsync<int>("modem.get_max_id");
    public Task<double> GetCarrierAsync() => Client.CallAsync("modem.get_carrier").GetValueAsync<double>("modem.get_carrier");
    public Task<double> GetQualityAsync() => Client.CallAsync("modem.get_quality").GetValueAsync<double>("modem.get_quality");
    public Task<object?> IncAfcSearchRangeAsync(int increment) => Client.CallAsync("modem.inc_afc_search_range", increment).GetValueAsync<object?>("modem.inc_afc_search_range");
    public Task<object?> IncBandwidthAsync(int increment) => Client.CallAsync("modem.inc_bandwidth", increment).GetValueAsync<object?>("modem.inc_bandwidth");
    public Task<int> IncCarrierAsync(int increment) => Client.CallAsync("modem.inc_carrier", increment).GetValueAsync<int>("modem.inc_carrier");
    public Task<int> OliviaGetBandwidthAsync() => Client.CallAsync("modem.olivia.get_bandwidth").GetValueAsync<int>("modem.olivia.get_bandwidth");
    public Task<int> OliviaGetTonesAsync() => Client.CallAsync("modem.olivia.get_tones").GetValueAsync<int>("modem.olivia.get_tones");
    public Task<object?> OliviaSetBandwidthAsync(int bandwidth) => Client.CallAsync("modem.olivia.set_bandwidth", bandwidth).GetValueAsync<object?>("modem.olivia.set_bandwidth");
    public Task<object?> OliviaSetTonesAsync(int tones) => Client.CallAsync("modem.olivia.set_tones", tones).GetValueAsync<object?>("modem.olivia.set_tones");
    public Task<object?> SearchDownAsync() => Client.CallAsync("modem.search_down").GetValueAsync<object?>("modem.search_down");
    public Task<object?> SearchUpAsync() => Client.CallAsync("modem.search_up").GetValueAsync<object?>("modem.search_up");
    public Task<object?> SetAfcSearchRangeAsync(int range) => Client.CallAsync("modem.set_afc_search_range", range).GetValueAsync<object?>("modem.set_afc_search_range");
    public Task<object?> SetBandwidthAsync(int bandwidth) => Client.CallAsync("modem.set_bandwidth", bandwidth).GetValueAsync<object?>("modem.set_bandwidth");
    public Task<int> SetByIdAsync(int id) => Client.CallAsync("modem.set_by_id", id).GetValueAsync<int>("modem.set_by_id");
    public Task<object?> SetByNameAsync(string name) => Client.CallAsync("modem.set_by_name", name).GetValueAsync<object?>("modem.set_by_name");
    public Task<object?> SetCarrierAsync(int carrier) => Client.CallAsync("modem.set_carrier", carrier).GetValueAsync<object?>("modem.set_carrier");
    public Task<object?> SetCarrierAsync(double carrier) => Client.CallAsync("modem.set_carrier", carrier).GetValueAsync<object?>("modem.set_carrier");
}