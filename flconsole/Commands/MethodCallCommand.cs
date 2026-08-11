using System.IO;
using flconsole.Models;

namespace flconsole.Commands;

public sealed class MethodCallCommand(XmlRpcClient client) : ICommand<IReadOnlyList<string>>
{
    public string CommandName => "method";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        if (request.Count == 0)
        {
            return CommandTextStream.Create("Usage: method <method-name> [arg1 arg2 ...]");
        }

        var methodName = request[0];
        var parameters = request.Skip(1).Select(XmlRpcValueHelper.ParseParameter).ToList();
        try
        {
            var xmlRpcRequest = new XmlRpcRequest
            {
                MethodName = methodName,
                Parameters = parameters
            };

            var response = await client.SendAsync(xmlRpcRequest);
            return CommandTextStream.Create(XmlRpcValueHelper.FormatValue(response.Value));
        }
        catch (Exception ex)
        {
            return CommandTextStream.Create($"Error: {ex.Message}");
        }
    }
}
