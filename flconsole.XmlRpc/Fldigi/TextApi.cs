namespace flconsole.XmlRpc;

public sealed class TextApi : Api
{
    internal TextApi(XmlRpcClient client) : base(client) { }

	public Task<object?> AddTxAsync(string text) => Client.CallAsync("text.add_tx", text).GetValueAsync<object?>("text.add_tx");
	public Task<object?> AddTxBytesAsync(byte[] bytes) => Client.CallAsync("text.add_tx_bytes", bytes).GetValueAsync<object?>("text.add_tx_bytes");
	public Task<object?> ClearRxAsync() => Client.CallAsync("text.clear_rx").GetValueAsync<object?>("text.clear_rx");
	public Task<object?> ClearTxAsync() => Client.CallAsync("text.clear_tx").GetValueAsync<object?>("text.clear_tx");
	public Task<object?> GetRxAsync(int start, int length) => Client.CallAsync("text.get_rx", start, length).GetValueAsync<object?>("text.get_rx");
	public Task<int> GetRxLengthAsync() => Client.CallAsync("text.get_rx_length").GetValueAsync<int>("text.get_rx_length");
}