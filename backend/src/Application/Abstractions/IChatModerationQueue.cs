using NabTeams.Domain.Enums;

namespace NabTeams.Application.Abstractions;

public record ChatModerationWorkItem(Guid MessageId, string UserId, RoleChannel Channel, string Content);

public interface IChatModerationQueue
{
    ValueTask EnqueueAsync(ChatModerationWorkItem workItem, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatModerationWorkItem> DequeueAsync(CancellationToken cancellationToken);
}
