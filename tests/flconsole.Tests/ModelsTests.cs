using System.Xml.Linq;

namespace flconsole.Tests;

public class ModelsTests
{
    [Fact]
    public void XmlRpcRequest_PropertiesRoundTrip()
    {
        var request = new XmlRpcRequest
        {
            MethodName = "rig.get_mode",
            Parameters = ["USB", 14074000, true, 2.5]
        };

        Assert.Equal("rig.get_mode", request.MethodName);
        Assert.Equal(4, request.Parameters.Count);
        Assert.Equal("USB", request.Parameters[0]);
        Assert.Equal(14074000, request.Parameters[1]);
        Assert.Equal(true, request.Parameters[2]);
        Assert.Equal(2.5, request.Parameters[3]);
    }

    [Fact]
    public void XmlRpcRequest_CreateValueNode_HandlesDictionaryAndEnumerableAndFallback()
    {
        var request = new XmlRpcRequest
        {
            Parameters =
            [
                new Dictionary<string, object?>
                {
                    ["name"] = "Ada",
                    ["enabled"] = true,
                    ["count"] = 3
                },
                new object?[] { "x", 1, null },
                new CustomValue("fallback")
            ]
        };

        var dictionary = Assert.IsType<Dictionary<string, object?>>(request.Parameters[0]);
        Assert.Equal("Ada", dictionary["name"]);
        Assert.True(Assert.IsType<bool>(dictionary["enabled"]));
        Assert.Equal(3, Assert.IsType<int>(dictionary["count"]));

        var array = Assert.IsType<List<object?>>(request.Parameters[1]);
        Assert.Equal("x", array[0]);
        Assert.Equal(1, array[1]);
        Assert.Equal(string.Empty, array[2]);

        Assert.Equal("fallback", request.Parameters[2]);
    }

    [Fact]
    public void XmlRpcRequest_ToAndFromXDocument_RoundTrips()
    {
        var request = new XmlRpcRequest
        {
            MethodName = "demo.call",
            Parameters = ["hello", 7, false]
        };

        var xml = request.ToXDocument();
        var parsed = XmlRpcRequest.FromXDocument(xml);

        Assert.Equal("demo.call", parsed.MethodName);
        Assert.Equal("hello", parsed.Parameters[0]);
        Assert.Equal(7, parsed.Parameters[1]);
        Assert.False(Assert.IsType<bool>(parsed.Parameters[2]));
    }

    [Fact]
    public void XmlRpcRequest_FromXDocument_ThrowsWhenRootMissing()
    {
        var document = new XDocument();

        var exception = Assert.Throws<InvalidOperationException>(() => XmlRpcRequest.FromXDocument(document));
        Assert.Equal("XML-RPC request is missing a root element.", exception.Message);
    }

    [Fact]
    public void XmlRpcRequest_FromXDocument_UsesDefaultsWhenNodesMissing()
    {
        var document = XDocument.Parse("<methodCall />");

        var parsed = XmlRpcRequest.FromXDocument(document);

        Assert.Equal(string.Empty, parsed.MethodName);
        Assert.Empty(parsed.Parameters);
    }

    [Fact]
    public void XmlRpcResponse_ValueSetter_ReplacesExistingValueAndClearsOnNull()
    {
        var response = new XmlRpcResponse { Value = "first" };

        response.Value = 42;
        Assert.Equal(42, response.Value);
        Assert.Single(response.Parameters);

        response.Value = null;
        Assert.Null(response.Value);
        Assert.Empty(response.Parameters);
    }

    [Fact]
    public void XmlRpcResponse_ToAndFromXDocument_RoundTrips()
    {
        var response = new XmlRpcResponse { Value = "done" };

        var xml = response.ToXDocument();
        var parsed = XmlRpcResponse.FromXDocument(xml);

        Assert.Equal("done", parsed.Value);
    }

    [Fact]
    public void XmlRpcResponse_FromXDocument_ThrowsWhenRootMissing()
    {
        var document = new XDocument();

        var exception = Assert.Throws<InvalidOperationException>(() => XmlRpcResponse.FromXDocument(document));
        Assert.Equal("XML-RPC response is missing a root element.", exception.Message);
    }

    [Fact]
    public void XmlRpcResponse_FromXDocument_UsesEmptyParametersWhenMissing()
    {
        var document = XDocument.Parse("<methodResponse />");

        var parsed = XmlRpcResponse.FromXDocument(document);

        Assert.Empty(parsed.Parameters);
        Assert.Null(parsed.Value);
    }

    [Fact]
    public void XmlRpcValue_FromXml_ParsesI4AndUnknownAndMissingChild()
    {
        var i4Value = XmlRpcValue.FromXml(XElement.Parse("<value><i4>9</i4></value>"));
        Assert.Equal(9, Assert.IsType<int>(i4Value!.GetValue()));

        var base64Value = XmlRpcValue.FromXml(XElement.Parse("<value><base64>SGVsbG8=</base64></value>"));
        Assert.Equal("Hello", Encoding.UTF8.GetString(Assert.IsType<byte[]>(base64Value!.GetValue())));

        var unknownType = XmlRpcValue.FromXml(XElement.Parse("<value><dateTime.iso8601>20240101T000000</dateTime.iso8601></value>"));
        Assert.Equal("20240101T000000", Assert.IsType<string>(unknownType!.GetValue()));

        var noChild = XmlRpcValue.FromXml(XElement.Parse("<value>literal-text</value>"));
        Assert.Equal("literal-text", Assert.IsType<string>(noChild!.GetValue()));
    }

    [Fact]
    public void XmlRpcValue_FromXml_HandlesStructMemberMissingValueNode()
    {
        var value = XmlRpcValue.FromXml(XElement.Parse("""
<value>
  <struct>
    <member>
      <name>key</name>
    </member>
  </struct>
</value>
"""));

        var structure = Assert.IsType<Dictionary<string, object?>>(value!.GetValue());
        Assert.Equal(string.Empty, Assert.IsType<string>(structure["key"]));
    }

    private sealed record CustomValue(string Text)
    {
        public override string ToString() => Text;
    }
}