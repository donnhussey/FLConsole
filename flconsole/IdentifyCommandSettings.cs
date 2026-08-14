namespace flconsole;

public sealed record IdentifyCommandSettings(
	IReadOnlyList<string> Modems,
	double MinCarrierOffsetHz = 1,
	double MaxCarrierOffsetHz = 3000,
	double ModemCarrierOffset = 1500,
	int DefaultRsidListenSeconds = 5,
	int DefaultTopCandidates = 5,
	double MinimumQualityToIdentify = 5,
	int FrequencyCarrierSettleDelayMilliseconds = 150,
	int RsidSampleIntervalMilliseconds = 500,
	int ModeSettleDelayMilliseconds = 300,
	int HeuristicQualitySampleDelayMilliseconds = 1500);