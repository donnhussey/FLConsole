namespace flconsole.XmlRpc;

public sealed class RxtxApi : Api
{
    internal RxtxApi(XmlRpcClient client) : base(client) { }

	public Task<object?> GetDataAsync() => Client.CallAsync("rxtx.get_data").GetValueAsync<object?>("rxtx.get_data");
}