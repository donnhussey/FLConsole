namespace flconsole.XmlRpc;

public sealed class IoApi : Api
{
    internal IoApi(XmlRpcClient client) : base(client) { }

	public Task<object?> EnableArqAsync() => Client.CallAsync("io.enable_arq").GetValueAsync<object?>("io.enable_arq");
	public Task<object?> EnableKissAsync() => Client.CallAsync("io.enable_kiss").GetValueAsync<object?>("io.enable_kiss");
	public Task<string> InUseAsync() => Client.CallAsync("io.in_use").GetValueAsync<string>("io.in_use");
}