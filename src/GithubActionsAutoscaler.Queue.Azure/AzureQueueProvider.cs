using Azure.Storage.Queues;
using GithubActionsAutoscaler.Abstractions.Queue;

namespace GithubActionsAutoscaler.Queue.Azure;

public class AzureQueueProvider : IQueueProvider
{
	private readonly QueueClient _queueClient;

	public AzureQueueProvider(AzureQueueOptions options)
	{
		_queueClient = new QueueClient(options.ConnectionString, options.QueueName);
	}

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
	}

	public async Task<IQueueMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default)
	{
		var response = await _queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
		if (response.Value == null)
		{
			return null;
		}

		return new AzureQueueMessage(
			response.Value.MessageId,
			response.Value.PopReceipt,
			response.Value.MessageText,
			response.Value.InsertedOn,
			checked((int)response.Value.DequeueCount)
		);
	}

	public async Task<IQueueMessage?> PeekMessageAsync(CancellationToken cancellationToken = default)
	{
		var response = await _queueClient.PeekMessageAsync(cancellationToken);
		if (response.Value == null)
		{
			return null;
		}

		return new AzureQueueMessage(
			response.Value.MessageId,
			string.Empty,
			response.Value.MessageText,
			response.Value.InsertedOn,
			checked((int)response.Value.DequeueCount)
		);
	}

	public async Task SendMessageAsync(string content, CancellationToken cancellationToken = default)
	{
		await _queueClient.SendMessageAsync(content, cancellationToken);
	}

	public async Task DeleteMessageAsync(IQueueMessage message, CancellationToken cancellationToken = default)
	{
		await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
	}

	public async Task<int> GetApproximateMessageCountAsync(
		CancellationToken cancellationToken = default)
	{
		var properties = await _queueClient.GetPropertiesAsync(cancellationToken: cancellationToken);
		return properties.Value.ApproximateMessagesCount;
	}
}
