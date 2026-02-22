using Shared.Models;

namespace MyBudgetIA.Application.Photo.Dtos.Blob
{
    /// <summary>
    /// Represents the result of a blob upload operation.
    /// </summary>
    /// <remarks>
    /// If the upload fails, error details are provided in the ErrorMessage and ErrorCode properties.
    /// When IsSucess is <see langword="false"/>, Etag, and LastModifiedAt may not be meaningful.
    /// </remarks>
    public sealed class BlobUploadResult()
    {      
        /// <summary>
        /// Gets the entity tag (ETag) value used for identifying the version of the resource.
        /// </summary>
        public string? Etag { get; init; }

        /// <summary>
        /// Gets the date and time, in Coordinated Universal Time (UTC), when the entity was last modified.
        /// </summary>
        public DateTimeOffset? LastModifiedUtc { get; init; }

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
            string etag,
            DateTimeOffset? lastModifiedUtc = null)
            => new()
            {
                Etag = etag,
                LastModifiedUtc = lastModifiedUtc,
                IsSuccess = true
            };

        /// <summary>
        /// Creates a failed upload result.
        /// </summary>
        public static BlobUploadResult CreateFailure(
            string errorMessage,
            string? errorCode = null)
            => new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
    }
}
