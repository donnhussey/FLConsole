using System.Net.Http.Headers;
using System.Text;
using flconsole.Models;

namespace flconsole;

public sealed class XmlRpcClient(string host, int port, HttpClient? httpClient = null)
{
    public string Host { get; } = host;
    public int Port { get; } = port;

    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

    public async Task<XmlRpcResponse> SendAsync(XmlRpcRequest request)
    {
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
