using Azure;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using MyBudgetIA.Infrastructure.Storage.Blob;
using Shared.Models;

namespace MyBudgetIA.Infrastructure.Storage.Queue
{
    /// <summary>
    /// Provides functionality to map Azure Queue storage exceptions to standardized error codes for queue operations.
    /// </summary>
    /// <remarks>This class implements the IAzureErrorMapper interface and is intended to translate exceptions
    /// thrown by Azure Queue operations into user-friendly error codes. It is typically used to ensure consistent error
    /// handling and reporting across different queue storage operations. The mapping covers common Azure Queue error
    /// scenarios, such as not found, already exists, unauthorized access, throttling, and service
    /// unavailability.</remarks>
    public sealed class AzureQueueErrorMapper : IAzureStorageErrorMapper
    {
        /// <summary>
        /// Maps a storage-related exception to a standardized error code based on the exception details and the type of
        /// storage operation performed.
        /// </summary>
        /// <param name="ex">The exception that contains information about the failure encountered during a storage operation. Cannot be
        /// null.</param>
        /// <param name="operationType">The type of storage operation that was being performed when the exception occurred. Determines the specific
        /// error code returned for certain failure scenarios.</param>
        /// <returns>A string representing the error code that corresponds to the provided exception and operation type. The
        /// returned code indicates the nature of the failure for consistent error handling.</returns>
        public string Map(RequestFailedException ex, StorageOperationType operationType)
        {
            if (ex.Status == 404)
            {
                return ex.ErrorCode switch
                {
                    "QueueNotFound" => ErrorCodes.QueueNotFound,
                    "MessageNotFound" => ErrorCodes.QueueMessageNotFound,
                    _ => ErrorCodes.QueueMessageNotFound
                };
            }

            if (ex.Status == 409 && ex.ErrorCode == "QueueAlreadyExists")
                return ErrorCodes.QueueAlreadyExists;

            if (ex.Status is 401 or 403)
                return ErrorCodes.QueueUnauthorized;

            if (ex.Status == 429)
                return ErrorCodes.QueueThrottled;

            if (ex.Status >= 500)
                return ErrorCodes.QueueUnavailable;

            return operationType switch
            {
                StorageOperationType.QueueMessageSending => ErrorCodes.QueueMessageSendingFailed,
                StorageOperationType.QueueMessageReceiving => ErrorCodes.QueueMessageReceivingFailed,
                _ => ErrorCodes.QueueOperationFailed
            };
        }
    }
}
