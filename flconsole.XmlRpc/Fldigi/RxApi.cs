namespace flconsole.XmlRpc;

public sealed class RxApi : Api
{
    internal RxApi(XmlRpcClient client) : base(client) { }

    public Task<object?> GetDataAsync() => Client.CallAsync("rx.get_data").GetValueAsync<object?>("rx.get_data");
}