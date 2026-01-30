namespace Shared.Models
{
    public static class ErrorCodes
    {
        // Validation
        public const string ValidationError = "VALIDATION_ERROR";

        public const string InternalError = "INTERNAL_ERROR";

        public const string BadRequest = "BAD_REQUEST";

        public const string MaxPhotoCountExceeded = "MAX_PHOTO_COUNT_EXCEEDED";

        public const string BlobStorageError = "BLOB_STORAGE_ERROR";

    }
}
