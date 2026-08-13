namespace flconsole.XmlRpc;

public sealed class NavtexApi : Api
{
    internal NavtexApi(XmlRpcClient client) : base(client) { }

	public Task<string> GetMessageAsync(int delaySeconds) => Client.CallAsync("navtex.get_message", delaySeconds).GetValueAsync<string>("navtex.get_message");
	public Task<string> SendMessageAsync(string message) => Client.CallAsync("navtex.send_message", message).GetValueAsync<string>("navtex.send_message");
}