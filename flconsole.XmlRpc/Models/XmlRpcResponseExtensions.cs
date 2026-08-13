namespace flconsole.XmlRpc.Models;

internal static class XmlRpcResponseExtensions
{
    public static Task<T> GetValueAsync<T>(this Task<XmlRpcResponse> responseTask, string methodName)
    {
        return XmlRpcResponse.GetValueAsync<T>(responseTask, methodName);
    }
}