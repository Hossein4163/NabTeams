namespace NabTeams.Application.Abstractions;

public interface IMetricsRecorder
{
    void RecordModeration(TimeSpan duration, ModerationDecision decision, double riskScore);
    void RecordModerationFailure();
}
