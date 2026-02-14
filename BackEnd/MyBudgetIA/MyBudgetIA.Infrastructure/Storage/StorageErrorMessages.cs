namespace MyBudgetIA.Infrastructure.Storage
{
    /// <summary>
    /// Provides a centralized collection of error message constants used for validation and exception handling within
    /// the application.
    /// </summary>
    internal static class StorageErrorMessages
    {
        public const string UnexpectedUploadFailure = "Unexpected blob upload failure.";
        public const string ValidationFailed = "Blob upload request validation failed.";
        public const string AzureBlobUploadFailed = "Azure Blob upload failed.";

        public const string BlobNotFound = "Blob not found.";

        public const string BlobNameValidationFailed = "Blob name must not be empty.";
        public const string AzureDownloadFailed = "Unexpected blob download failure.";

    }
}
