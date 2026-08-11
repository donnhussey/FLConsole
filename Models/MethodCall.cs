namespace flconsole.Models;

public class MethodCall
{
    public string MethodName { get; set; } = string.Empty;

    public List<Parameter> Parameters { get; set; } = [];
}
