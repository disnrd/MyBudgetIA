namespace Shared.Models
{
    public static class ErrorCodes
    {
        // Validation
        public const string ValidationError = "VALIDATION_ERROR";

        public const string InternalError = "INTERNAL_ERROR";

        public const string BadRequest = "BAD_REQUEST";

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

    }
}
