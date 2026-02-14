using Azure;
using Shared.Models;

namespace MyBudgetIA.Infrastructure.Storage
{
    /// <summary>
    /// Provides standardized error code mappings for Azure Blob Storage operations.
    /// </summary>
    /// <remarks>This class is intended to facilitate consistent handling of Azure Blob Storage errors by
    /// mapping service-specific error codes to application-level error codes. It is typically used in scenarios where
    /// Azure Blob Storage exceptions need to be interpreted and translated for higher-level error handling or logging.
    /// The class is static and cannot be instantiated.</remarks>
    public static class AzureBlobErrorCodes
    {
        /// <summary>
        /// Maps an Azure Blob Storage error represented by a <see cref="RequestFailedException"/> to a standardized
        /// application error code.
        /// </summary>
        /// <remarks>Use this method to translate Azure Blob Storage errors into application-specific
        /// error codes for consistent error handling. The mapping covers common error scenarios such as blob already
        /// exists, container not found, unauthorized access, throttling, and service unavailability.</remarks>
        /// <param name="ex">The exception containing the Azure Blob Storage error information to be mapped. Cannot be null.</param>
        /// <param name="operationType">The type of blob operation being performed (e.g., upload, download) that may influence the error code mapping. Cannot be null.</param>
        /// <returns>A string representing the mapped application error code corresponding to the Azure Blob Storage error.
        /// Returns a specific error code for common error conditions, or a general error code if the error does not
        /// match known cases.</returns>
        public static string  MapAzureBlobErrorCode(RequestFailedException ex, BlobOperationType operationType)
        {
            if (ex.Status == 409 || string.Equals(ex.ErrorCode, "BlobAlreadyExists", StringComparison.OrdinalIgnoreCase))
            {
                return ErrorCodes.BlobAlreadyExists;
            }

            if (ex.Status == 404 || string.Equals(ex.ErrorCode, "ContainerNotFound", StringComparison.OrdinalIgnoreCase))
            {
                return ex.ErrorCode switch
                {
                    "ContainerNotFound" => ErrorCodes.BlobContainerNotFound,
                    "BlobNotFound" => ErrorCodes.BlobNotFound,
                    _ => ErrorCodes.BlobNotFound
                };
            }

            if (ex.Status is 401 or 403)
            {
                return ErrorCodes.BlobUnauthorized;
            }

            if (ex.Status == 429)
            {
                return ErrorCodes.BlobThrottled;
            }

            if (ex.Status >= 500)
            {
                return ErrorCodes.BlobUnavailable;
            }

            return operationType switch
            {
                BlobOperationType.Upload => ErrorCodes.BlobUploadFailed,
                BlobOperationType.Download => ErrorCodes.BlobDownloadFailed,

                _ => ErrorCodes.BlobOperationFailed
            };
        }
    }
}
