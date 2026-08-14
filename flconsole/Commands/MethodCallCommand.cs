using flconsole.XmlRpc.Models;

namespace flconsole.Commands;

public sealed class MethodCallCommand(FLDigi _fldigi, CommandMessages? messages = null) : ICommand
{
    private readonly CommandMessages _messages = messages ?? CommandMessages.Defaults;
    public string CommandName => "method";
    public bool Repeat => false;
    public TimeSpan RepeatInterval => TimeSpan.Zero;
    public bool StopsShell => false;

    public async Task ExecuteAsync(IReadOnlyList<string> request, ICommandOutput output, CancellationToken cancellationToken = default)
    {
        if (request.Count == 0)
        {
            await output.WriteAsync(_messages.MethodUsage, cancellationToken); return;
        }

        var methodName = request[0];
        var parameters = request.Skip(1).Select(XmlRpcValueHelper.ParseParameter).ToList();
        var response = await _fldigi.InvokeAsync(methodName, parameters.ToArray());
        await output.WriteAsync(XmlRpcValueHelper.FormatValue(response), cancellationToken);
    }
}
