using Azure.Storage.Queues;

namespace MyBudgetIA.Infrastructure.Storage.Abstractions
{
    /// <summary>
    /// Defines a contract for obtaining clients to interact with Azure Queue Storage services.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for configuring and authenticating access
    /// to Azure Queue Storage. Use this interface to acquire a QueueClient instance for performing queue operations
    /// such as sending, receiving, or managing messages.</remarks>
    public interface IAzureQueueServiceClient
    {
        /// <summary>
        /// Asynchronously sends a message to the queue and returns the unique message identifier along with the time
        /// the message was inserted.
        /// </summary>
        /// <param name="message">The content of the message to send. This parameter cannot be null or empty.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the message
        /// identifier and the insertion time of the message.</returns>
        Task<(string MessageId, DateTimeOffset InsertionTime)> SendMessageAsync(string message, CancellationToken ct);
    }
}
