using System.Net.Http;
using System.Reflection;

namespace flconsole.Tests;

public sealed class FLDigiApiTests
{
    [Fact]
    public void ComposesDocumentedNamespaces()
    {
        var fldigi = new FLDigi(
            new XmlRpcConnectionSettings("127.0.0.1", 7362),
            new HttpClient(new HttpClientHandler()));

        Assert.NotNull(fldigi.Io);
        Assert.NotNull(fldigi.Log);
        Assert.NotNull(fldigi.Main);
        Assert.NotNull(fldigi.Modem);
        Assert.NotNull(fldigi.Navtex);
        Assert.NotNull(fldigi.Rig);
        Assert.NotNull(fldigi.Rx);
        Assert.NotNull(fldigi.Rxtx);
        Assert.NotNull(fldigi.Spot);
        Assert.NotNull(fldigi.Text);
        Assert.NotNull(fldigi.Tx);
        Assert.NotNull(fldigi.Wefax);
    }

    [Fact]
    public void DeprecatedWrappersExposeMigrationDiagnostics()
    {
        var rsid = typeof(MainApi).GetMethod(nameof(MainApi.RsidAsync));
        var rigFrequency = typeof(MainApi).GetMethod(nameof(MainApi.GetRigFrequencyAsync));
        var sideband = typeof(MainApi).GetMethod(nameof(MainApi.GetSidebandAsync));

        Assert.Equal("FLDIGI001", rsid?.GetCustomAttribute<ObsoleteAttribute>()?.DiagnosticId);
        Assert.Contains("GetRsidAsync", rsid?.GetCustomAttribute<ObsoleteAttribute>()?.Message);
        Assert.Equal("FLDIGI006", rigFrequency?.GetCustomAttribute<ObsoleteAttribute>()?.DiagnosticId);
        Assert.Contains("GetFrequencyAsync", rigFrequency?.GetCustomAttribute<ObsoleteAttribute>()?.Message);
        Assert.Equal("FLDIGI002", sideband?.GetCustomAttribute<ObsoleteAttribute>()?.DiagnosticId);
    }

    [Fact]
    public void WefaxApiExposesDocumentedMethods()
    {
        var methods = typeof(WefaxApi)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            [
                nameof(WefaxApi.EndReceptionAsync),
                nameof(WefaxApi.GetReceivedFileAsync),
                nameof(WefaxApi.SendFileAsync),
                nameof(WefaxApi.SetAdifLogAsync),
                nameof(WefaxApi.SetMaxLinesAsync),
                nameof(WefaxApi.SetTxAbortFlagAsync),
                nameof(WefaxApi.SkipAptAsync),
                nameof(WefaxApi.SkipPhasingAsync),
                nameof(WefaxApi.StateStringAsync)
            ],
            methods.OrderBy(name => name).ToArray());

        Assert.Equal([typeof(int)], typeof(WefaxApi).GetMethod(nameof(WefaxApi.GetReceivedFileAsync))?.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal([typeof(string), typeof(int)], typeof(WefaxApi).GetMethod(nameof(WefaxApi.SendFileAsync))?.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal([typeof(bool)], typeof(WefaxApi).GetMethod(nameof(WefaxApi.SetAdifLogAsync))?.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal([typeof(int)], typeof(WefaxApi).GetMethod(nameof(WefaxApi.SetMaxLinesAsync))?.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void ApiObjectsExposeExpectedMethodCounts()
    {
        var expectedCounts = new Dictionary<Type, int>
        {
            [typeof(FLDigi)] = 8,
            [typeof(IoApi)] = 3,
            [typeof(LogApi)] = 28,
            [typeof(MainApi)] = 55,
            [typeof(ModemApi)] = 23,
            [typeof(NavtexApi)] = 2,
            [typeof(RigApi)] = 17,
            [typeof(RxApi)] = 1,
            [typeof(RxtxApi)] = 1,
            [typeof(SpotApi)] = 4,
            [typeof(TextApi)] = 6,
            [typeof(TxApi)] = 1,
            [typeof(WefaxApi)] = 9
        };

        foreach (var (apiType, expectedCount) in expectedCounts)
        {
            var actualCount = apiType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Count(method => method.ReturnType.IsGenericType
                    && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

            Assert.Equal(expectedCount, actualCount);
        }

        Assert.Equal(158, expectedCounts.Keys
            .SelectMany(apiType => apiType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Count(method => method.ReturnType.IsGenericType
                && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)));
    }
}
