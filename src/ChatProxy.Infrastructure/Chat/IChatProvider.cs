using ChatProxy.Domain.Chat;

namespace ChatProxy.Infrastructure.Chat
{
    public interface IChatProvider
    {
        Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct);
    }
}
