namespace flconsole.XmlRpc;

public sealed class LogApi : Api
{
    internal LogApi(XmlRpcClient client) : base(client) { }

    public Task<object?> ClearAsync() => Client.CallAsync("log.clear").GetValueAsync<object?>("log.clear");
    public Task<string> GetAzAsync() => Client.CallAsync("log.get_az").GetValueAsync<string>("log.get_az");
    public Task<string> GetBandAsync() => Client.CallAsync("log.get_band").GetValueAsync<string>("log.get_band");
    public Task<string> GetCallAsync() => Client.CallAsync("log.get_call").GetValueAsync<string>("log.get_call");
    public Task<string> GetCountryAsync() => Client.CallAsync("log.get_country").GetValueAsync<string>("log.get_country");
    public Task<string> GetExchangeAsync() => Client.CallAsync("log.get_exchange").GetValueAsync<string>("log.get_exchange");
    public Task<string> GetFrequencyAsync() => Client.CallAsync("log.get_frequency").GetValueAsync<string>("log.get_frequency");
    public Task<string> GetLocatorAsync() => Client.CallAsync("log.get_locator").GetValueAsync<string>("log.get_locator");
    public Task<string> GetNameAsync() => Client.CallAsync("log.get_name").GetValueAsync<string>("log.get_name");
    public Task<string> GetNotesAsync() => Client.CallAsync("log.get_notes").GetValueAsync<string>("log.get_notes");
    public Task<string> GetProvinceAsync() => Client.CallAsync("log.get_province").GetValueAsync<string>("log.get_province");
    public Task<string> GetQthAsync() => Client.CallAsync("log.get_qth").GetValueAsync<string>("log.get_qth");
    public Task<string> GetRstInAsync() => Client.CallAsync("log.get_rst_in").GetValueAsync<string>("log.get_rst_in");
    public Task<string> GetRstOutAsync() => Client.CallAsync("log.get_rst_out").GetValueAsync<string>("log.get_rst_out");
    public Task<string> GetSerialNumberAsync() => Client.CallAsync("log.get_serial_number").GetValueAsync<string>("log.get_serial_number");
    public Task<string> GetSerialNumberSentAsync() => Client.CallAsync("log.get_serial_number_sent").GetValueAsync<string>("log.get_serial_number_sent");
    public Task<string> GetStateAsync() => Client.CallAsync("log.get_state").GetValueAsync<string>("log.get_state");
    public Task<string> GetTimeOffAsync() => Client.CallAsync("log.get_time_off").GetValueAsync<string>("log.get_time_off");
    public Task<string> GetTimeOnAsync() => Client.CallAsync("log.get_time_on").GetValueAsync<string>("log.get_time_on");
    public Task<object?> SetCallAsync(string call) => Client.CallAsync("log.set_call", call).GetValueAsync<object?>("log.set_call");
    public Task<object?> SetExchangeAsync(string exchange) => Client.CallAsync("log.set_exchange", exchange).GetValueAsync<object?>("log.set_exchange");
    public Task<object?> SetLocatorAsync(string locator) => Client.CallAsync("log.set_locator", locator).GetValueAsync<object?>("log.set_locator");
    public Task<object?> SetNameAsync(string name) => Client.CallAsync("log.set_name", name).GetValueAsync<object?>("log.set_name");
    public Task<object?> SetQthAsync(string qth) => Client.CallAsync("log.set_qth", qth).GetValueAsync<object?>("log.set_qth");
    public Task<object?> SetRstInAsync(string rst) => Client.CallAsync("log.set_rst_in", rst).GetValueAsync<object?>("log.set_rst_in");
    public Task<object?> SetRstOutAsync(string rst) => Client.CallAsync("log.set_rst_out", rst).GetValueAsync<object?>("log.set_rst_out");
    public Task<object?> SetSerialNumberAsync(string serial) => Client.CallAsync("log.set_serial_number", serial).GetValueAsync<object?>("log.set_serial_number");

    [Obsolete("Use Main.GetWfSidebandAsync.", DiagnosticId = "FLDIGI015")]
    public Task<object?> GetSidebandAsync() => Client.CallAsync("log.get_sideband").GetValueAsync<object?>("log.get_sideband");
}