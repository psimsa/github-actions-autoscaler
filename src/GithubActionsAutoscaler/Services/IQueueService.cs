using GithubActionsAutoscaler.Models;

namespace GithubActionsAutoscaler.Services;

public interface IQueueService
{
    Task InitializeAsync(CancellationToken token);
    Task<QueueMessage?> ReceiveMessageAsync(CancellationToken token);
    Task DeleteMessageAsync(string messageId, string popReceipt, CancellationToken token);
    Task<string?> PeekMessageIdAsync(CancellationToken token);
}
