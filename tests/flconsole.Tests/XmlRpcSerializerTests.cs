using System.Xml.Linq;
using flconsole;
using flconsole.Models;
using Xunit;

namespace flconsole.Tests;

public class XmlRpcSerializerTests
{
    [Fact]
    public void SerializeRequest_UsesDoubleForFrequencyParameter()
    {
        var request = new XmlRpcRequest
        {
            MethodName = "main.set_frequency",
            Parameters = [14074000.0]
        };

        var xml = XmlRpcSerializer.SerializeRequest(request);
        var document = XDocument.Parse(xml);

        Assert.Equal("methodCall", document.Root?.Name.LocalName);
        Assert.Equal("main.set_frequency", document.Root?.Element("methodName")?.Value);
        Assert.Equal("double", document.Root?.Element("params")?.Element("param")?.Element("value")?.Elements().First().Name.LocalName);
        Assert.Equal("14074000", document.Root?.Element("params")?.Element("param")?.Element("value")?.Element("double")?.Value);
    }

    [Fact]
    public void SerializeRequest_ProducesXmlForSimpleAndCompositeParameters()
    {
        var request = new XmlRpcRequest
        {
            MethodName = "demo.call",
            Parameters = ["hello", 42, true, new object?[] { 1, "two", 3.5 }, new Dictionary<string, object?> { ["name"] = "Ada", ["active"] = false }]
        };

        var xml = XmlRpcSerializer.SerializeRequest(request);

        Assert.Contains("<methodName>demo.call</methodName>", xml);
        Assert.Contains("<string>hello</string>", xml);
        Assert.Contains("<int>42</int>", xml);
        Assert.Contains("<boolean>1</boolean>", xml);
        Assert.Contains("<array>", xml);
        Assert.Contains("<struct>", xml);
        Assert.Contains("<name>name</name>", xml);
        Assert.Contains("<string>Ada</string>", xml);
    }

    [Fact]
    public void SerializeRequest_SerializesAllXmlRpcValueKinds()
    {
        var request = new XmlRpcRequest
        {
            MethodName = "demo.call",
            Parameters =
            [
                new XmlRpcStringValue { Text = "hello" },
                new XmlRpcIntValue { Text = 7 },
                new XmlRpcBooleanValue { Text = true },
                new XmlRpcDoubleValue { Text = 2.5 },
                new XmlRpcArrayValue
                {
                    Values =
                    [
                        new XmlRpcStringValue { Text = "item" },
                        new XmlRpcIntValue { Text = 9 }
                    ]
                },
                new XmlRpcStructValue
                {
                    Members =
                    [
                        new XmlRpcMember { Name = "count", Value = new XmlRpcIntValue { Text = 1 } },
                        new XmlRpcMember { Name = "enabled", Value = new XmlRpcBooleanValue { Text = false } }
                    ]
                }
            ]
        };

        var xml = XmlRpcSerializer.SerializeRequest(request);

        Assert.Contains("<string>hello</string>", xml);
        Assert.Contains("<int>7</int>", xml);
        Assert.Contains("<boolean>1</boolean>", xml);
        Assert.Contains("<double>2.5</double>", xml);
        Assert.Contains("<array>", xml);
        Assert.Contains("<struct>", xml);
        Assert.Contains("<name>count</name>", xml);
        Assert.Contains("<name>enabled</name>", xml);
    }

    [Fact]
    public void DeserializeResponse_ParsesDoubleValueFromXmlRpcReply()
    {
        const string response = """
<?xml version="1.0"?>
<methodResponse>
  <params>
    <param>
      <value><double>14074000</double></value>
    </param>
  </params>
</methodResponse>
""";

        var parsed = XmlRpcSerializer.DeserializeResponse(response);

        Assert.IsType<double>(parsed.Value);
        Assert.Equal(14074000d, (double)parsed.Value!);
    }

    [Fact]
    public void DeserializeResponse_ParsesScalarAndCompositeValues()
    {
        const string response = """
<methodResponse>
  <params>
    <param><value><string>hello</string></value></param>
    <param><value><int>7</int></value></param>
    <param><value><boolean>1</boolean></value></param>
    <param><value><double>2.5</double></value></param>
    <param>
      <value>
        <array>
          <data>
            <value><string>item</string></value>
            <value><int>9</int></value>
          </data>
        </array>
      </value>
    </param>
    <param>
      <value>
        <struct>
          <member><name>count</name><value><int>1</int></value></member>
          <member><name>enabled</name><value><boolean>0</boolean></value></member>
        </struct>
      </value>
    </param>
  </params>
</methodResponse>
""";

        var parsed = XmlRpcSerializer.DeserializeResponse(response);
        var values = parsed.MethodResponse.Parameters.Select(parameter => parameter.Value?.GetValue()).ToList();

        Assert.Equal("hello", values[0]);
        Assert.Equal(7, values[1]);
        Assert.True(Assert.IsType<bool>(values[2]));
        Assert.Equal(2.5d, Assert.IsType<double>(values[3]));

        var arrayValues = Assert.IsType<List<object?>>(values[4]);
        Assert.Equal("item", Assert.IsType<string>(arrayValues[0]));
        Assert.Equal(9, Assert.IsType<int>(arrayValues[1]));

        var structure = Assert.IsType<Dictionary<string, object?>>(values[5]);
        Assert.Equal(1, structure["count"]);
        Assert.False(Assert.IsType<bool>(structure["enabled"]));
    }

    [Fact]
    public void DeserializeResponse_ParsesNestedArrayAndStructValues()
    {
        const string response = """
<methodResponse>
  <params>
    <param>
      <value>
        <array>
          <data>
            <value><string>hello</string></value>
            <value><boolean>1</boolean></value>
          </data>
        </array>
      </value>
    </param>
  </params>
</methodResponse>
""";

        var parsed = XmlRpcSerializer.DeserializeResponse(response);
        var values = Assert.IsType<List<object?>>(parsed.Value);

        Assert.Equal("hello", Assert.IsType<string>(values[0]));
        Assert.True(Assert.IsType<bool>(values[1]));
    }

    [Fact]
    public void ParseParameter_ConvertsStringTokensToXmlRpcFriendlyValues()
    {
        Assert.Equal(42, XmlRpcValueHelper.ParseParameter("42"));
        Assert.Equal(3.5, Assert.IsType<double>(XmlRpcValueHelper.ParseParameter("3.5")));
        Assert.Equal("hello", XmlRpcValueHelper.ParseParameter("hello"));
    }

    [Fact]
    public void FormatValue_ProducesReadableStringsForCommonValues()
    {
        Assert.Equal("null", XmlRpcValueHelper.FormatValue(null));
        Assert.Equal("hello", XmlRpcValueHelper.FormatValue("hello"));
        Assert.Equal("[1,2,3]", XmlRpcValueHelper.FormatValue(new object?[] { 1, 2, 3 }));
    }

}
