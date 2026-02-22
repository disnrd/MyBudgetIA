using Azure.Storage.Queues;

namespace MyBudgetIA.Infrastructure.Storage.Abstractions
{
    /// <summary>
    /// Provides an adapter that enables sending messages to an Azure Queue using the specified QueueClient instance.
    /// </summary>
    /// <param name="queueClient">The QueueClient instance used to interact with the Azure Queue service.</param>
    public class AzureQueueServiceClientAdapter(QueueClient queueClient) : IAzureQueueServiceClient
    {
        /// <summary>
        /// Asynchronously sends a message to the queue and returns the unique identifier and insertion time of the
        /// enqueued message.
        /// </summary>
        /// <param name="message">The content of the message to enqueue. This parameter cannot be null or empty.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A tuple containing the unique message identifier and the time at which the message was inserted into the
        /// queue.</returns>
        public async Task<(string MessageId, DateTimeOffset InsertionTime)> SendMessageAsync(string message, CancellationToken ct)
        {
            var response = await queueClient.SendMessageAsync(message, ct).ConfigureAwait(false);
            return (response.Value.MessageId, response.Value.InsertionTime);
        }
    }
}
