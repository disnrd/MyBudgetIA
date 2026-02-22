using Azure;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using Shared.Models;
namespace MyBudgetIA.Infrastructure.Storage.Blob
{
    /// <summary>
    /// Provides functionality to map exceptions from Azure Blob Storage operations to standardized error codes.
    /// </summary>
    /// <remarks>This class implements the IAzureErrorMapper interface to translate
    /// Azure.RequestFailedException instances into user-friendly error codes based on the type of storage operation and
    /// the specific error encountered. It is intended to simplify error handling and reporting for consumers of Azure
    /// Blob Storage by providing consistent error codes for common failure scenarios.</remarks>
    public sealed class AzureBlobErrorMapper : IAzureStorageErrorMapper
    {
        /// <summary>
        /// Maps a RequestFailedException to a standardized error code based on the exception details and the type of
        /// storage operation performed.
        /// </summary>
        /// <param name="ex">The exception that contains information about the failure encountered during a storage operation.</param>
        /// <param name="operationType">The type of storage operation that was being performed when the exception occurred. This value determines
        /// the specific error code returned for general failures.</param>
        /// <returns>A string representing the error code that corresponds to the provided exception and operation type. The
        /// returned code indicates the nature of the failure for further handling or display.</returns>
        public string Map(RequestFailedException ex, StorageOperationType operationType)
        {
            if (ex.Status == 409 || ex.ErrorCode == "BlobAlreadyExists")
                return ErrorCodes.BlobAlreadyExists;

            if (ex.Status == 404)
            {
                return ex.ErrorCode switch
                {
                    "ContainerNotFound" => ErrorCodes.BlobContainerNotFound,
                    "BlobNotFound" => ErrorCodes.BlobNotFound,
                    _ => ErrorCodes.BlobNotFound
                };
            }

            if (ex.Status is 401 or 403)
                return ErrorCodes.BlobUnauthorized;

            if (ex.Status == 429)
                return ErrorCodes.BlobThrottled;

            if (ex.Status >= 500)
                return ErrorCodes.BlobUnavailable;

            return operationType switch
            {
                StorageOperationType.BlobUpload => ErrorCodes.BlobUploadFailed,
                StorageOperationType.BlobDownload => ErrorCodes.BlobDownloadFailed,
                _ => ErrorCodes.BlobOperationFailed
            };
        }
    }

}
