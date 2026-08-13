namespace flconsole.XmlRpc;

public sealed class WefaxApi : Api
{
    internal WefaxApi(XmlRpcClient client) : base(client) { }

	public Task<string> EndReceptionAsync() => Client.CallAsync("wefax.end_reception").GetValueAsync<string>("wefax.end_reception");
	public Task<string> GetReceivedFileAsync(int delaySeconds) => Client.CallAsync("wefax.get_received_file", delaySeconds).GetValueAsync<string>("wefax.get_received_file");
	public Task<string> SendFileAsync(string fileName, int delaySeconds) => Client.CallAsync("wefax.send_file", fileName, delaySeconds).GetValueAsync<string>("wefax.send_file");
	public Task<string> SetAdifLogAsync(bool enabled) => Client.CallAsync("wefax.set_adif_log", enabled).GetValueAsync<string>("wefax.set_adif_log");
	public Task<string> SetMaxLinesAsync(int maxLines) => Client.CallAsync("wefax.set_max_lines", maxLines).GetValueAsync<string>("wefax.set_max_lines");
	public Task<string> SetTxAbortFlagAsync() => Client.CallAsync("wefax.set_tx_abort_flag").GetValueAsync<string>("wefax.set_tx_abort_flag");
	public Task<string> SkipAptAsync() => Client.CallAsync("wefax.skip_apt").GetValueAsync<string>("wefax.skip_apt");
	public Task<string> SkipPhasingAsync() => Client.CallAsync("wefax.skip_phasing").GetValueAsync<string>("wefax.skip_phasing");
	public Task<string> StateStringAsync() => Client.CallAsync("wefax.state_string").GetValueAsync<string>("wefax.state_string");
}