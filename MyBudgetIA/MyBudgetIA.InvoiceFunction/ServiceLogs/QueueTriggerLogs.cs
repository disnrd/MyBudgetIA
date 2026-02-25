using Microsoft.Extensions.Logging;

namespace MyBudgetIA.InvoiceFunction.ServiceLogs
{

    /// <summary>
    /// Provides logging utilities and message templates for queue trigger operations in the invoice function.
    /// </summary>
    internal static partial class QueueTriggerLogs
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Queue trigger function processed for message '{MessageId}'.")]
        public static partial void LogStartedInvoiceProcess(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Queue message received. Blob={BlobName}, TrackingId={TrackingId}, DequeueCount={DequeueCount}")]
        public static partial void LogBeginScopeContext(this ILogger logger, string blobName, string trackingId, long dequeueCount);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Queue message processed successfully")]
        public static partial void LogInvoiceProcessSuccess(this ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Functional error while processing message '{MessageId}'. Message will NOT be retried. Exception :{exceptionMessage}")]
        public static partial void LogInvoiceProcessFunctionalError(this ILogger logger, string messageId, string exceptionMessage);

        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Transient error while processing message '{MessageId}'. Message will be retried.")]
        public static partial void LogInvoiceProcessTransientError(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 6, Level = LogLevel.Critical, Message = "Unexpected error while processing message '{MessageId}'. Message will be retried. Exception :{exceptionMessage}")]
        public static partial void LogInvoiceProcessUnexpectedError(this ILogger logger, string messageId, string exceptionMessage);

        [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Invoice process cancelled. MessageId={MessageId}")]
        public static partial void LogInvoiceProcessCanceled(this ILogger logger, string messageId);
    }
}
