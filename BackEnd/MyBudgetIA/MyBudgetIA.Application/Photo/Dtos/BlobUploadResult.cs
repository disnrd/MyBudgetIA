using Shared.Models;

namespace MyBudgetIA.Application.Photo.Dtos
{
    /// <summary>
    /// Represents the result of a blob upload operation, including status, metadata, and error details if applicable.
    /// </summary>
    /// <remarks>
    /// If the upload fails, error details are provided in the ErrorMessage and ErrorCode properties.
    /// When IsSucess is <see langword="false"/>, BlobName, Etag, and LastModifiedAt may not be meaningful.
    /// </remarks>
    /// <param name="blobName">The unique name assigned to the uploaded blob. This value is set if the upload succeeds.</param>
    /// <param name="trackingId">The tracking ID associated with the upload operation, used for tracking and logging.</param>
    public sealed class BlobUploadResult(string blobName, string trackingId)
    {
        /// <summary>
        /// The original file name of the uploaded file.
        /// </summary>
        public string FileName { get; init; } = string.Empty;

        /// <summary>
        /// The unique blob name assigned to the uploaded file (if successful).
        /// </summary>
        public string BlobName { get; init; } = blobName;

        /// <summary>
        /// Gets the unique identifier used to track the operation or request.
        /// </summary>
        public string TrackingId { get; init; } = trackingId;

        /// <summary>
        /// Gets the entity tag (ETag) value used for identifying the version of the resource.
        /// </summary>
        public string? Etag { get; init; }

        /// <summary>
        /// Gets the date and time, in Coordinated Universal Time (UTC), when the entity was last modified.
        /// </summary>
        public DateTimeOffset? LastModifiedUtc { get; init; }

        /// <summary>
        /// Gets the date and time, in Coordinated Universal Time (UTC), when the upload occurred.
        /// </summary>
        public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets a value indicating whether the operation completed successfully.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Error message if upload failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Error code if upload failed (for programmatic handling).
        /// </summary>
        public string? ErrorCode { get; init; }

        /// <summary>
        /// Creates a successful upload result.
        /// </summary>
        public static BlobUploadResult CreateSuccess(
            string fileName,
            string trackingId,
            string blobName,
            string etag,
            DateTimeOffset? lastModifiedUtc = null)
            => new(blobName, trackingId)
            {
                FileName = fileName,
                TrackingId = trackingId,
                Etag = etag,
                LastModifiedUtc = lastModifiedUtc,
                IsSuccess = true
            };

        /// <summary>
        /// Creates a failed upload result.
        /// </summary>
        public static BlobUploadResult CreateFailure(
            string fileName,
            string trackingId,
            string blobName,
            string errorMessage,
            string? errorCode = null)
            => new(blobName, trackingId)
            {
                FileName = fileName,
                TrackingId = trackingId,
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };

        /// <summary>
        /// Creates a failed upload result representing an application-level validation error for a single file.
        /// This is useful for batch uploads where invalid files should not stop other uploads.
        /// </summary>
        public static BlobUploadResult CreateValidationFailure(
            string fileName,
            string errorMessage)
            => CreateFailure(
                fileName: fileName,
                trackingId: string.Empty,
                blobName: string.Empty,
                errorMessage: errorMessage,
                errorCode: ErrorCodes.ValidationError);
    }
}
