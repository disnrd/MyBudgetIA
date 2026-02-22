using MyBudgetIA.Application.Photo.Dtos.Queue;
using Shared.Storage.DTOS;

namespace MyBudgetIA.Application.Interfaces
{
    /// <summary>
    /// Contract for enqueueing messages to a queue storage (Azure Storage Queues).
    /// </summary>
    public interface IQueueStorageService
    {
        /// <summary>
        /// Enqueues a message for asynchronous processing in the queue storage system.
        /// </summary>
        /// <param name="request">The request containing the details of the message to be enqueued. This parameter must not be null and should
        /// include valid message data.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the enqueue operation. The default value is <see
        /// cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous enqueue operation. The task result contains a <see
        /// cref="QueuePushResult"/> indicating whether the message was successfully enqueued.</returns>
        Task<QueuePushResult> EnqueueAsync(QueueMessageRequest request, CancellationToken ct = default);
    }
}
