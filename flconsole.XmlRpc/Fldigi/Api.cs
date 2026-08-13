namespace flconsole.XmlRpc;

public abstract class Api
{
    internal XmlRpcClient Client { get; }

    internal Api(XmlRpcClient client)
    {
        Client = client;
    }
}