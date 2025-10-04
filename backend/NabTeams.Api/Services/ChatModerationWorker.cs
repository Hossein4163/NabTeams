using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NabTeams.Api.Hubs;
using NabTeams.Api.Models;
using NabTeams.Api.Stores;

namespace NabTeams.Api.Services;

public class ChatModerationWorker : BackgroundService
{
    private readonly IChatModerationQueue _queue;
    private readonly IModerationService _moderationService;
    private readonly IChatRepository _chatRepository;
    private readonly IModerationLogStore _moderationLogStore;
    private readonly IUserDisciplineStore _userDisciplineStore;
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    private readonly ILogger<ChatModerationWorker> _logger;
    private readonly IMetricsRecorder _metrics;

    public ChatModerationWorker(
        IChatModerationQueue queue,
        IModerationService moderationService,
        IChatRepository chatRepository,
        IModerationLogStore moderationLogStore,
        IUserDisciplineStore userDisciplineStore,
        IHubContext<ChatHub, IChatClient> hubContext,
        ILogger<ChatModerationWorker> logger,
        IMetricsRecorder metrics)
    {
        _queue = queue;
        _moderationService = moderationService;
        _chatRepository = chatRepository;
        _moderationLogStore = moderationLogStore;
        _userDisciplineStore = userDisciplineStore;
        _hubContext = hubContext;
        _logger = logger;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(workItem, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process moderation work item for message {MessageId}", workItem.MessageId);
                _metrics.RecordModerationFailure();
            }
        }
    }

    private async Task ProcessAsync(ChatModerationWorkItem workItem, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var candidate = new MessageCandidate(workItem.UserId, workItem.Channel, workItem.Content);
        var moderation = await _moderationService.ModerateAsync(candidate, cancellationToken);

        var status = moderation.Decision switch
        {
            ModerationDecision.Publish or ModerationDecision.SoftWarn => MessageStatus.Published,
            ModerationDecision.Hold => MessageStatus.Held,
            _ => MessageStatus.Blocked
        };

        await _chatRepository.UpdateMessageModerationAsync(
            workItem.MessageId,
            status,
            moderation.RiskScore,
            moderation.PolicyTags,
            moderation.Notes,
            moderation.PenaltyPoints,
            cancellationToken);

        var message = await _chatRepository.GetMessageAsync(workItem.MessageId, cancellationToken);
        if (message is null)
        {
            _logger.LogWarning("Message {MessageId} disappeared before broadcasting moderation result", workItem.MessageId);
            return;
        }

        var log = new ModerationLog
        {
            MessageId = workItem.MessageId,
            UserId = workItem.UserId,
            Channel = workItem.Channel,
            RiskScore = moderation.RiskScore,
            PolicyTags = moderation.PolicyTags,
            ActionTaken = moderation.Decision.ToString(),
            PenaltyPoints = moderation.PenaltyPoints
        };
        await _moderationLogStore.AddAsync(log, cancellationToken);

        if (moderation.PenaltyPoints != 0)
        {
            await _userDisciplineStore.UpdateScoreAsync(
                workItem.UserId,
                workItem.Channel,
                -moderation.PenaltyPoints,
                moderation.Notes,
                workItem.MessageId,
                cancellationToken);
        }

        await _hubContext.Clients.User(workItem.UserId).MessageUpserted(message);

        if (status == MessageStatus.Published)
        {
            await _hubContext.Clients.Group(ChatHub.BuildGroupName(workItem.Channel)).MessageUpserted(message);
        }

        var elapsed = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency);
        _metrics.RecordModeration(elapsed, moderation.Decision, moderation.RiskScore);
    }
}
