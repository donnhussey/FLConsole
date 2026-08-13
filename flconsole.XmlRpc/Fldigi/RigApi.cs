namespace flconsole.XmlRpc;

public sealed class RigApi : Api
{
    internal RigApi(XmlRpcClient client) : base(client) { }

    public Task<object?> TakeControlAsync() => Client.CallAsync("rig.take_control").GetValueAsync<object?>("rig.take_control");
    public Task<double> GetFrequencyAsync() => Client.CallAsync("rig.get_frequency").GetValueAsync<double>("rig.get_frequency");
    public Task<object?> SetFrequencyAsync(double frequency) => Client.CallAsync("rig.set_frequency", frequency).GetValueAsync<object?>("rig.set_frequency");
    public Task<object?> GetModeAsync() => Client.CallAsync("rig.get_mode").GetValueAsync<object?>("rig.get_mode");
    public Task<object?> SetModeAsync(string mode) => Client.CallAsync("rig.set_mode", mode).GetValueAsync<object?>("rig.set_mode");
    public Task<object?> GetBandwidthAsync() => Client.CallAsync("rig.get_bandwidth").GetValueAsync<object?>("rig.get_bandwidth");
    public Task<object?> GetBandwidthsAsync() => Client.CallAsync("rig.get_bandwidths").GetValueAsync<object?>("rig.get_bandwidths");
    public Task<object?> SetBandwidthAsync(string bandwidth) => Client.CallAsync("rig.set_bandwidth", bandwidth).GetValueAsync<object?>("rig.set_bandwidth");
    public Task<object?> SetBandwidthsAsync(IEnumerable<string> bandwidths) => Client.CallAsync("rig.set_bandwidths", bandwidths.ToArray()).GetValueAsync<object?>("rig.set_bandwidths");
    public Task<object?> GetModesAsync() => Client.CallAsync("rig.get_modes").GetValueAsync<object?>("rig.get_modes");
    public Task<object?> SetModesAsync(IEnumerable<string> modes) => Client.CallAsync("rig.set_modes", modes.ToArray()).GetValueAsync<object?>("rig.set_modes");
    public Task<object?> SetNameAsync(string name) => Client.CallAsync("rig.set_name", name).GetValueAsync<object?>("rig.set_name");
    public Task<string> GetNameAsync() => Client.CallAsync("rig.get_name").GetValueAsync<string>("rig.get_name");
    public Task<string> GetNotchAsync() => Client.CallAsync("rig.get_notch").GetValueAsync<string>("rig.get_notch");
    public Task<object?> ReleaseControlAsync() => Client.CallAsync("rig.release_control").GetValueAsync<object?>("rig.release_control");
    public Task<object?> SetPwrmeterAsync(int value) => Client.CallAsync("rig.set_pwrmeter", value).GetValueAsync<object?>("rig.set_pwrmeter");
    public Task<object?> SetSmeterAsync(int value) => Client.CallAsync("rig.set_smeter", value).GetValueAsync<object?>("rig.set_smeter");
}