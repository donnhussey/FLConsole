using System.Net.Http.Headers;
using System.Text;
using flconsole.XmlRpc.Models;

namespace flconsole.XmlRpc;

internal sealed class XmlRpcClient(string Host, int Port, HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    internal async Task<XmlRpcResponse> CallAsync(string methodName, params object?[] parameters)
    {
        var request = new XmlRpcRequest
        {
            MethodName = methodName,
            Parameters = parameters.ToList()
        };
        var requestBody = XmlRpcSerializer.SerializeRequest(request);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"http://{Host}:{Port}/")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "text/xml")
        };

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

        using var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        return XmlRpcSerializer.DeserializeResponse(responseBody);
    }
}
