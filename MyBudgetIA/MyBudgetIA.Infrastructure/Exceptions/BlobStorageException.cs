using Shared.Models;

namespace MyBudgetIA.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception thrown when an error occurs during Azure Blob Storage operations.
    /// </summary>
    public sealed class BlobStorageException : InfrastructureException
    {
        private const string DefaultPublicMessage = "Blob storage unavailable.";

        /// <summary>
        /// Gets the name of the blob that caused the error, if applicable.
        /// </summary>
        public string? BlobName { get; }

        /// <summary>
        /// Gets the Azure-specific error code, if available.
        /// </summary>
        public string? AzureErrorCode { get; }

        /// <summary>
        /// Gets the HTTP status code returned by the Azure service, if available.
        /// </summary>
        public int? AzureStatusCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStorageException"/> class.
        /// </summary>
        public BlobStorageException(
            string message,
            string? blobName = null,
            Exception? innerException = null)
            : base(
                publicMessage: DefaultPublicMessage,
                errorCode: ErrorCodes.BlobStorageError,
                statusCode: 503,
                internalMessage: message,
                innerException: innerException)
        {
            BlobName = blobName;

            if (innerException is Azure.RequestFailedException azureEx)
            {
                AzureErrorCode = azureEx.ErrorCode;
                AzureStatusCode = azureEx.Status;
            }
        }

        /// <summary>
        /// Initializes a new instance with explicit Azure error code.
        /// </summary>
        public BlobStorageException(
            string message,
            string? blobName,
            string? azureErrorCode,
            int? azureStatusCode = null,
            Exception? innerException = null)
            : base(
                publicMessage: DefaultPublicMessage,
                errorCode: azureErrorCode ?? ErrorCodes.BlobStorageError,
                statusCode: azureStatusCode ?? 503,
                internalMessage: message,
                innerException: innerException)
        {
            BlobName = blobName;
            AzureErrorCode = azureErrorCode;
            AzureStatusCode = azureStatusCode;
        }
    }
}
