using Microsoft.Extensions.Logging;

namespace MyBudgetIA.Infrastructure.Storage
{
    /// <summary>
    /// Provides extension methods for logging operations related to Azure Blob Storage service activities.
    /// </summary>
    /// <remarks>This static class contains logging helpers intended for use with structured logging
    /// frameworks. The methods are designed to log key events during blob storage operations, enabling consistent and
    /// informative log output across applications.</remarks>
    internal static partial class BlobStorageServiceLogs
    {
        /// <summary>
        /// Logs an informational message indicating that a blob upload has started for the specified blob and
        /// container.
        /// </summary>
        /// <param name="logger">The logger instance used to record the upload start message.</param>
        /// <param name="blobName">The name of the blob being uploaded.</param>
        /// <param name="containerName">The name of the container to which the blob is being uploaded.</param>
        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Uploading blob {BlobName} to container {ContainerName}.")]
        public static partial void LogStartedUploadingBlob(this ILogger logger, string blobName, string containerName);

        /// <summary>
        /// Logs an informational message indicating that a blob upload request validation has failed.
        /// </summary>
        /// <remarks>This method is intended for use in scenarios where blob upload requests do not meet
        /// validation requirements. The log entry is written at the Information level and can be used for auditing or
        /// troubleshooting failed upload attempts.</remarks>
        /// <param name="logger">The logger instance used to record the validation failure event.</param>
        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Blob upload request validation failed.")]
        public static partial void LogFailedBlobRequestValidation(this ILogger logger);

        /// <summary>
        /// Logs an informational message indicating that a blob was successfully uploaded to a container, including the
        /// blob name, tracking identifier, and container name.
        /// </summary>
        /// <param name="logger">The logger instance used to record the success message.</param>
        /// <param name="blobName">The name of the blob that was uploaded.</param>
        /// <param name="trackingId">The tracking identifier associated with the upload operation.</param>
        /// <param name="containerName">The name of the container to which the blob was uploaded.</param>
        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Succesfully uploade blob {BlobName} with id {TrackingId} to container {ContainerName}.")]
        public static partial void LogSuccessUploadBlob(this ILogger logger, string blobName, string trackingId, string containerName);

        /// <summary>
        /// Logs an informational message indicating that an Azure Blob upload operation has failed, including the
        /// status and error code returned by the service.
        /// </summary>
        /// <param name="logger">The logger instance used to write the log message.</param>
        /// <param name="blobName">The name of the blob that failed to upload.</param>
        /// <param name="status">The status returned by the Azure Blob service for the failed upload operation.</param>
        /// <param name="errorCode">The error code provided by the Azure Blob service describing the reason for the failure.</param>
        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Azure Blob upload failed for blob {BlobName}. Status={Status}, ErrorCode={ErrorCode}.")]
        public static partial void LogAzureBlobUploadError(this ILogger logger, string blobName, int status, string errorCode);

        /// <summary>
        /// Logs an error indicating that an unexpected failure occurred during an Azure Blob upload operation.
        /// </summary>
        /// <param name="logger">The logger instance used to write the error message.</param>
        /// <param name="blobName">The name of the blob for which the upload failed. Cannot be null.</param>
        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Unexpected blob upload failure. BlobName: {BlobName}.")]
        public static partial void LogAzureBlobUploadUnexpectedError(this ILogger logger, string blobName);

    }
}
