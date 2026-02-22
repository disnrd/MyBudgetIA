namespace Shared.Models
{
    public static class ErrorCodes
    {
        // Validation
        public const string ValidationError = "VALIDATION_ERROR";

        public const string InternalError = "INTERNAL_ERROR";

        public const string BadRequest = "BAD_REQUEST";

        public const string NotFound = "NOT_FOUND";

        public const string MaxPhotoCountExceeded = "MAX_PHOTO_COUNT_EXCEEDED";

        // INFRA

        public const string BlobStorageError = "BLOB_STORAGE_ERROR";

        public const string BlobAlreadyExists = "BLOB_ALREADY_EXISTS";

        public const string BlobContainerNotFound = "BLOB_CONTAINER_NOT_FOUND";

        public const string BlobUnauthorized = "BLOB_UNAUTHORIZED";

        public const string BlobThrottled = "BLOB_THROTTLED";

        public const string BlobUnavailable = "BLOB_UNAVAILABLE";

        public const string BlobUploadFailed = "BLOB_UPLOAD_FAILED";

        public const string BlobValidationFailed = "BLOB_VALIDATION_FAILED";

        public const string BlobNotFound = "BLOB_NOT_FOUND";

        public const string BlobDownloadFailed = "BLOB_DOWNLOAD_FAILED";

        public const string BlobOperationFailed = "BLOB_OPERATION_FAILED";

        public const string BlobStorageValidationError = "BLOB_STORAGE_VALIDATION_ERROR";


        public const string QueueMessageSendingFailed = "QUEUE_MESSAGE_SENDING_FAILED";

        public const string QueueValidationFailed = "QUEUE_VALIDATION_FAILED";

        public const string QueueStorageError= "QUEUE_STORAGE_ERROR";

        public const string QueuePushFailed = "QUEUE_PUSH_FAILED";

        public const string QueuePushUnexpectedFailed = "QUEUE_PUSH_UNEXPECTED_FAILED";

        public const string QueueRequestValidationFailed = "QUEUE_REQUEST_VALIDATION_FAILED";

        public const string QueueMessageNotFound = "QUEUE_MESSAGE_NOT_FOUND";

        public const string QueueNotFound = "QUEUE_NOT_FOUND";

        public const string QueueAlreadyExists = "QUEUE_ALREADY_EXISTS";

        public const string QueueUnauthorized = "QUEUE_UNAUTHORIZED";
 
        public const string QueueThrottled = "QUEUE_THROTTLED";

        public const string QueueUnavailable = "QUEUE_UNAVAILABLE";

        public const string QueueMessageReceivingFailed = "QUEUE_MESSAGE_RECEIVING_FAILED";

        public const string QueueOperationFailed = "QUEUE_OPERATION_FAILED";

        public const string QueueMessageSerializationError = "FAILED_TO_SERIALIZE_QUEUE_MESSAGE";
    }
}
