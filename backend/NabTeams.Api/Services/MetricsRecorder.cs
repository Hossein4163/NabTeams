using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using NabTeams.Api.Models;

namespace NabTeams.Api.Services;

public class MetricsRecorder : IMetricsRecorder, IDisposable
{
    public const string MeterName = "NabTeams.Api";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private readonly Histogram<double> _moderationDuration = Meter.CreateHistogram<double>(
        "moderation_duration_ms",
        unit: "ms",
        description: "Latency of moderation decisions");

    private readonly Counter<long> _moderationTotal = Meter.CreateCounter<long>(
        "moderation_total",
        description: "Total moderation decisions processed");

    private readonly Histogram<double> _moderationRisk = Meter.CreateHistogram<double>(
        "moderation_risk_score",
        description: "Distribution of moderation risk scores");

    private readonly Counter<long> _moderationFailures = Meter.CreateCounter<long>(
        "moderation_failures_total",
        description: "Moderation operations that failed");

    public void RecordModeration(TimeSpan duration, ModerationDecision decision, double riskScore)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("decision", decision.ToString())
        };

        _moderationDuration.Record(duration.TotalMilliseconds, tags);
        _moderationRisk.Record(riskScore, tags);
        _moderationTotal.Add(1, tags);
    }

    public void RecordModerationFailure()
    {
        _moderationFailures.Add(1);
    }

    public void Dispose()
    {
        Meter.Dispose();
    }
}
