namespace flconsole.XmlRpc;

public sealed class MainApi : Api
{
    internal MainApi(XmlRpcClient client) : base(client) { }

    public Task<object?> AbortAsync() => Client.CallAsync("main.abort").GetValueAsync<object?>("main.abort");
    public Task<bool> GetAfcAsync() => Client.CallAsync("main.get_afc").GetValueAsync<bool>("main.get_afc");
    public Task<string> GetCharRatesAsync() => Client.CallAsync("main.get_char_rates").GetValueAsync<string>("main.get_char_rates");
    public Task<object?> GetCharTimingAsync(byte[] character) => Client.CallAsync("main.get_char_timing", character).GetValueAsync<object?>("main.get_char_timing");
    public Task<double> GetFrequencyAsync() => Client.CallAsync("main.get_frequency").GetValueAsync<double>("main.get_frequency");
    public Task<bool> GetLockAsync() => Client.CallAsync("main.get_lock").GetValueAsync<bool>("main.get_lock");
    public Task<int> GetMaxMacroIdAsync() => Client.CallAsync("main.get_max_macro_id").GetValueAsync<int>("main.get_max_macro_id");
    public Task<bool> GetReverseAsync() => Client.CallAsync("main.get_reverse").GetValueAsync<bool>("main.get_reverse");
    public Task<bool> GetRsidAsync() => Client.CallAsync("main.get_rsid").GetValueAsync<bool>("main.get_rsid");
    public Task<bool> GetSquelchAsync() => Client.CallAsync("main.get_squelch").GetValueAsync<bool>("main.get_squelch");
    public Task<double> GetSquelchLevelAsync() => Client.CallAsync("main.get_squelch_level").GetValueAsync<double>("main.get_squelch_level");
    public Task<string> GetStatus1Async() => Client.CallAsync("main.get_status1").GetValueAsync<string>("main.get_status1");
    public Task<string> GetStatus2Async() => Client.CallAsync("main.get_status2").GetValueAsync<string>("main.get_status2");
    public Task<string> GetTrxStateAsync() => Client.CallAsync("main.get_trx_state").GetValueAsync<string>("main.get_trx_state");
    public Task<string> GetTrxStatusAsync() => Client.CallAsync("main.get_trx_status").GetValueAsync<string>("main.get_trx_status");
    public Task<bool> GetTxIdAsync() => Client.CallAsync("main.get_txid").GetValueAsync<bool>("main.get_txid");
    public Task<object?> GetTxTimingAsync(byte[] testString) => Client.CallAsync("main.get_tx_timing", testString).GetValueAsync<object?>("main.get_tx_timing");
    public Task<double> IncFrequencyAsync(double increment) => Client.CallAsync("main.inc_frequency", increment).GetValueAsync<double>("main.inc_frequency");
    public Task<double> IncSquelchLevelAsync(double increment) => Client.CallAsync("main.inc_squelch_level", increment).GetValueAsync<double>("main.inc_squelch_level");
    public Task<object?> RunMacroAsync(int macroId) => Client.CallAsync("main.run_macro", macroId).GetValueAsync<object?>("main.run_macro");
    public Task<object?> RxAsync() => Client.CallAsync("main.rx").GetValueAsync<object?>("main.rx");
    public Task<object?> RxOnlyAsync() => Client.CallAsync("main.rx_only").GetValueAsync<object?>("main.rx_only");
    public Task<object?> RxTxAsync() => Client.CallAsync("main.rx_tx").GetValueAsync<object?>("main.rx_tx");
    public Task<bool> SetAfcAsync(bool enabled) => Client.CallAsync("main.set_afc", enabled).GetValueAsync<bool>("main.set_afc");
    public Task<double> SetFrequencyAsync(double frequency) => Client.CallAsync("main.set_frequency", frequency).GetValueAsync<double>("main.set_frequency");
    public Task<bool> SetLockAsync(bool enabled) => Client.CallAsync("main.set_lock", enabled).GetValueAsync<bool>("main.set_lock");
    public Task<bool> SetReverseAsync(bool enabled) => Client.CallAsync("main.set_reverse", enabled).GetValueAsync<bool>("main.set_reverse");
    public Task<object?> SetRsidAsync(bool enabled) => Client.CallAsync("main.set_rsid", enabled).GetValueAsync<object?>("main.set_rsid");
    public Task<bool> SetSquelchAsync(bool enabled) => Client.CallAsync("main.set_squelch", enabled).GetValueAsync<bool>("main.set_squelch");
    public Task<object?> SetSquelchLevelAsync(double level) => Client.CallAsync("main.set_squelch_level", level).GetValueAsync<object?>("main.set_squelch_level");
    public Task<bool> SetTxIdAsync(bool enabled) => Client.CallAsync("main.set_txid", enabled).GetValueAsync<bool>("main.set_txid");
    public Task<bool> ToggleAfcAsync() => Client.CallAsync("main.toggle_afc").GetValueAsync<bool>("main.toggle_afc");
    public Task<bool> ToggleLockAsync() => Client.CallAsync("main.toggle_lock").GetValueAsync<bool>("main.toggle_lock");
    public Task<bool> ToggleReverseAsync() => Client.CallAsync("main.toggle_reverse").GetValueAsync<bool>("main.toggle_reverse");
    public Task<object?> ToggleRsidAsync() => Client.CallAsync("main.toggle_rsid").GetValueAsync<object?>("main.toggle_rsid");
    public Task<bool> ToggleSquelchAsync() => Client.CallAsync("main.toggle_squelch").GetValueAsync<bool>("main.toggle_squelch");
    public Task<bool> ToggleTxIdAsync() => Client.CallAsync("main.toggle_txid").GetValueAsync<bool>("main.toggle_txid");
    public Task<object?> TuneAsync() => Client.CallAsync("main.tune").GetValueAsync<object?>("main.tune");
    public Task<object?> TxAsync() => Client.CallAsync("main.tx").GetValueAsync<object?>("main.tx");
    public Task<object?> GetWfSidebandAsync() => Client.CallAsync("main.get_wf_sideband").GetValueAsync<object?>("main.get_wf_sideband");
    public Task<object?> SetWfSidebandAsync(string sideband) => Client.CallAsync("main.set_wf_sideband", sideband).GetValueAsync<object?>("main.set_wf_sideband");

    [Obsolete("Use GetRsidAsync, SetRsidAsync, or ToggleRsidAsync.", DiagnosticId = "FLDIGI001")]
    public Task<object?> RsidAsync() => Client.CallAsync("main.rsid").GetValueAsync<object?>("main.rsid");
    [Obsolete("Use GetWfSidebandAsync and/or the rig mode methods.", DiagnosticId = "FLDIGI002")]
    public Task<object?> GetSidebandAsync() => Client.CallAsync("main.get_sideband").GetValueAsync<object?>("main.get_sideband");
    [Obsolete("Use SetWfSidebandAsync and/or the rig mode methods.", DiagnosticId = "FLDIGI003")]
    public Task<object?> SetSidebandAsync(string sideband) => Client.CallAsync("main.set_sideband", sideband).GetValueAsync<object?>("main.set_sideband");
    [Obsolete("Use the corresponding RigApi method.", DiagnosticId = "FLDIGI004")]
    public Task<object?> GetRigModeAsync() => Client.CallAsync("main.get_rig_mode").GetValueAsync<object?>("main.get_rig_mode");
    [Obsolete("Use Rig.GetModeAsync.", DiagnosticId = "FLDIGI005")]
    public Task<object?> SetRigModeAsync(string mode) => Client.CallAsync("main.set_rig_mode", mode).GetValueAsync<object?>("main.set_rig_mode");
    [Obsolete("Use Rig.GetBandwidthAsync.", DiagnosticId = "FLDIGI008")]
    public Task<object?> GetRigBandwidthAsync() => Client.CallAsync("main.get_rig_bandwidth").GetValueAsync<object?>("main.get_rig_bandwidth");
    [Obsolete("Use Rig.GetBandwidthsAsync.", DiagnosticId = "FLDIGI009")]
    public Task<object?> GetRigBandwidthsAsync() => Client.CallAsync("main.get_rig_bandwidths").GetValueAsync<object?>("main.get_rig_bandwidths");
    [Obsolete("Use Rig.SetBandwidthAsync.", DiagnosticId = "FLDIGI010")]
    public Task<object?> SetRigBandwidthAsync(string bandwidth) => Client.CallAsync("main.set_rig_bandwidth", bandwidth).GetValueAsync<object?>("main.set_rig_bandwidth");
    [Obsolete("Use Rig.SetBandwidthsAsync.", DiagnosticId = "FLDIGI011")]
    public Task<object?> SetRigBandwidthsAsync(IEnumerable<string> bandwidths) => Client.CallAsync("main.set_rig_bandwidths", bandwidths.ToArray()).GetValueAsync<object?>("main.set_rig_bandwidths");
    [Obsolete("Use Rig.GetModesAsync.", DiagnosticId = "FLDIGI012")]
    public Task<object?> GetRigModesAsync() => Client.CallAsync("main.get_rig_modes").GetValueAsync<object?>("main.get_rig_modes");
    [Obsolete("Use Rig.SetModesAsync.", DiagnosticId = "FLDIGI013")]
    public Task<object?> SetRigModesAsync(IEnumerable<string> modes) => Client.CallAsync("main.set_rig_modes", modes.ToArray()).GetValueAsync<object?>("main.set_rig_modes");
    [Obsolete("Use Rig.SetNameAsync.", DiagnosticId = "FLDIGI014")]
    public Task<object?> SetRigNameAsync(string name) => Client.CallAsync("main.set_rig_name", name).GetValueAsync<object?>("main.set_rig_name");
    [Obsolete("Use Rig.GetFrequencyAsync.", DiagnosticId = "FLDIGI006")]
    public Task<object?> GetRigFrequencyAsync() => Client.CallAsync("main.get_rig_frequency").GetValueAsync<object?>("main.get_rig_frequency");
    [Obsolete("Use Rig.SetFrequencyAsync.", DiagnosticId = "FLDIGI007")]
    public Task<object?> SetRigFrequencyAsync(double frequency) => Client.CallAsync("main.set_rig_frequency", frequency).GetValueAsync<object?>("main.set_rig_frequency");
}