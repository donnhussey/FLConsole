namespace flconsole.XmlRpc;

public sealed class SpotApi : Api
{
    internal SpotApi(XmlRpcClient client) : base(client) { }

	public Task<bool> GetAutoAsync() => Client.CallAsync("spot.get_auto").GetValueAsync<bool>("spot.get_auto");
	public Task<int> PskrepGetCountAsync() => Client.CallAsync("spot.pskrep.get_count").GetValueAsync<int>("spot.pskrep.get_count");
	public Task<object?> SetAutoAsync(bool enabled) => Client.CallAsync("spot.set_auto", enabled).GetValueAsync<object?>("spot.set_auto");
	public Task<object?> ToggleAutoAsync(bool enabled) => Client.CallAsync("spot.toggle_auto", enabled).GetValueAsync<object?>("spot.toggle_auto");
}