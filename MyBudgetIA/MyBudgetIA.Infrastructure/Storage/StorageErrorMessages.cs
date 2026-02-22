namespace MyBudgetIA.Infrastructure.Storage
{
    /// <summary>
    /// Provides a centralized collection of error message constants used for validation and exception handling within
    /// the application.
    /// </summary>
    internal static class StorageErrorMessages
    {
        public const string UnexpectedUploadFailure = "Unexpected blob upload failure.";
        public const string BlobRequestValidationFailed = "Blob upload request validation failed.";
        public const string BlobUploadFailed = "Azure Blob upload failed.";

        public const string BlobNotFound = "Blob not found.";

        public const string BlobNameValidationFailed = "Blob name must not be empty.";
        public const string BlobDownloadFailed = "Unexpected blob download failure.";

        public const string BlobsListFailed = "Unexpected blob listing failure.";

        public const string QueueRequestValidationFailed = "Queue message request validation failed.";
        public const string QueuePushFailed = "Failed to send message to the queue.";
        public const string QueuePushUnexpectedFailed = "Unexpected failure when sending message to the queue.";
        public const string FailedToSerializeQueueMessage = "Failed to serialize queue message.";
    }
}
