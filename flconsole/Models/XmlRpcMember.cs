namespace flconsole.Models;

public sealed class XmlRpcMember
{
    public string Name { get; set; } = string.Empty;

    public XmlRpcValue? Value { get; set; }
}
