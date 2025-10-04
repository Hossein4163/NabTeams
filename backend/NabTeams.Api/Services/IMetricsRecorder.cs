using System;
using NabTeams.Api.Models;

namespace NabTeams.Api.Services;

public interface IMetricsRecorder
{
    void RecordModeration(TimeSpan duration, ModerationDecision decision, double riskScore);
    void RecordModerationFailure();
}
