using System.Globalization;
using System.IO;
using System.Text;
using flconsole.Models;

namespace flconsole.Commands;

public sealed class ScanCommand(XmlRpcClient client) : ICommand<IReadOnlyList<string>>
{
    private const double DefaultScanStepHz = 50;
    private const double DefaultQualityThreshold = 20;
    private static readonly TimeSpan SweepRepeatInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan FrequencySettleDelay = TimeSpan.FromMilliseconds(250);

    public string CommandName => "scan";
    public bool Repeat => true;
    public TimeSpan RepeatInterval => SweepRepeatInterval;
    public bool StopsShell => false;

    public async Task<Stream> ExecuteAsync(IReadOnlyList<string> request)
    {
        if (request.Count < 2
            || request.Count > 4
            || !double.TryParse(request[0], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var firstBound)
            || !double.TryParse(request[1], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var secondBound)
            || !TryParseOptionalPositiveDouble(request, 2, DefaultScanStepHz, out var scanStepHz)
            || !TryParseOptionalPositiveDouble(request, 3, DefaultQualityThreshold, out var qualityThreshold))
        {
            return CommandTextStream.Create("Usage: scan <lower-frequency> <upper-frequency> [step-hz] [quality-threshold]");
        }

        var lowerBound = Math.Min(firstBound, secondBound);
        var upperBound = Math.Max(firstBound, secondBound);

        try
        {
            var output = new StringBuilder();

            await client.SendAsync(new XmlRpcRequest
            {
                MethodName = "rig.take_control",
                Parameters = []
            });

            for (var frequency = lowerBound; frequency <= upperBound; frequency += scanStepHz)
            {
                await client.SendAsync(new XmlRpcRequest
                {
                    MethodName = "rig.set_frequency",
                    Parameters = [frequency]
                });

                await Task.Delay(FrequencySettleDelay);

                var quality = await GetDoubleValueAsync("modem.get_quality");
                if (quality > qualityThreshold)
                {
                    output.AppendLine($"Activity at {frequency.ToString("0.###", CultureInfo.InvariantCulture)} Hz (quality={quality.ToString("0.###", CultureInfo.InvariantCulture)})");
                }
            }

            return CommandTextStream.Create(output.ToString().TrimEnd());
        }
        catch (Exception ex)
        {
            return CommandTextStream.Create($"Error: {ex.Message}");
        }
    }

    private static bool TryParseOptionalPositiveDouble(IReadOnlyList<string> request, int index, double defaultValue, out double value)
    {
        if (request.Count <= index)
        {
            value = defaultValue;
            return true;
        }

        if (!double.TryParse(request[index], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        return value > 0 || (index == 3 && value >= 0);
    }

    private async Task<double> GetDoubleValueAsync(string methodName)
    {
        var response = await client.SendAsync(new XmlRpcRequest
        {
            MethodName = methodName,
            Parameters = []
        });

        return response.Value switch
        {
            double doubleValue => doubleValue,
            int intValue => intValue,
            string stringValue when double.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedValue) => parsedValue,
            _ => throw new InvalidOperationException($"{methodName} did not return a numeric value.")
        };
    }
}
