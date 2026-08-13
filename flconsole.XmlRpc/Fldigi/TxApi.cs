namespace flconsole.XmlRpc;

public sealed class TxApi : Api
{
    internal TxApi(XmlRpcClient client) : base(client) { }

	public Task<object?> GetDataAsync() => Client.CallAsync("tx.get_data").GetValueAsync<object?>("tx.get_data");
}