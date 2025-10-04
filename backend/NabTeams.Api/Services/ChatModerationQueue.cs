using System.Threading.Channels;
using NabTeams.Api.Models;

namespace NabTeams.Api.Services;

public record ChatModerationWorkItem(Guid MessageId, string UserId, RoleChannel Channel, string Content);

public interface IChatModerationQueue
{
    ValueTask EnqueueAsync(ChatModerationWorkItem workItem, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatModerationWorkItem> DequeueAsync(CancellationToken cancellationToken);
}

public class ChatModerationQueue : IChatModerationQueue
{
    private readonly Channel<ChatModerationWorkItem> _channel;

    public ChatModerationQueue()
    {
        var options = new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<ChatModerationWorkItem>(options);
    }

    public ValueTask EnqueueAsync(ChatModerationWorkItem workItem, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(workItem, cancellationToken);

    public IAsyncEnumerable<ChatModerationWorkItem> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
