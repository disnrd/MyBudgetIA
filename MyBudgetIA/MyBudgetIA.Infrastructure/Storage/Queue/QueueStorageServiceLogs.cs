using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyBudgetIA.Infrastructure.Storage.Queue
{
    /// <summary>
    /// Provides logging functionality for queue storage operations.
    /// </summary>
    internal static partial class QueueStorageServiceLogs
    {

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Starting sending message about blob {BlobName} with id {TrackingId} to queue.")]
        public static partial void LogStartedSendingMessage(this ILogger logger, string blobName, string trackingId);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed sending message about blob {BlobName} with id {TrackingId} to queue.")]
        public static partial void LogFailedSendingMessage(this ILogger logger, string blobName, string trackingId);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Succesfully sent message about blob {BlobName} with id {TrackingId} to queue.")]
        public static partial void LogSuccessSendingMessage(this ILogger logger, string blobName, string trackingId);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Azure queue failed for blob {BlobName}. Status={Status}, ErrorCode={ErrorCode}.")]
        public static partial void LogAzureQueueError(this ILogger logger, string blobName, int status, string errorCode);

        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Unexpected queue storage failure for blob: {BlobName}.")]
        public static partial void LogAzureQueueUnexpectedError(this ILogger logger, string blobName);

        [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Queue message request validation failed.")]
        public static partial void LogFailedQueueRequestValidation(this ILogger logger);

        [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Failed to serialize queue message for blob {BlobName}.")]
        public static partial void LogQueueMessageSerializationFailed(this ILogger logger, string blobName);
    }
}
