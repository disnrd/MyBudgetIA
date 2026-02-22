using MyBudgetIA.Application.Photo.Dtos.Blob;
using MyBudgetIA.Application.Photo.Dtos.Queue;

namespace MyBudgetIA.Application.Photo.Dtos
{
    /// <summary>
    /// Represents the result of a photo upload operation, including details about the uploaded file, the sending of a message to a queue,
    /// the outcome of each processing step, and any associated errors.
    /// </summary>
    public sealed class UploadPhotosResult(
        string blobName,
        string fileName,
        string trackingId,
        string contentType)
    {
        /// <summary>
        /// The unique blob name assigned to the uploaded file (if successful).
        /// </summary>
        public string BlobName { get; init; } = blobName;

        /// <summary>
        /// The original file name of the uploaded file.
        /// </summary>
        public string FileName { get; init; } = fileName;

        /// <summary>
        /// Gets the unique identifier used to track the operation or request.
        /// </summary>
        public string TrackingId { get; init; } = trackingId;

        /// <summary>
        /// Gets the media type of the content represented by the instance.
        /// </summary>
        public string ContentType { get; init; } = contentType;

        /// <summary>
        /// Gets the date and time, in Coordinated Universal Time (UTC), when the upload started.
        /// </summary>
        public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the per-file blob upload results.
        /// </summary>
        public BlobUploadResult BlobResult { get; set; } = new();

        /// <summary>
        /// Gets the per-file queue sending message results.
        /// </summary>
        public QueuePushResult QueueResult { get; set; } = new();

        /// <summary>
        /// Validation / business errors (actionnable by the frontend).
        /// </summary>
        public ExplicitError[] Errors { get; init; } = [];

        /// <summary>
        /// Gets a value indicating whether all steps succeeded.
        /// </summary>
        public bool IsSuccess => BlobResult.IsSuccess && QueueResult.IsSuccess;

        /// <summary>
        /// Creates an instance of the <see cref="UploadPhotosResult"/> class that represents a validation failure for a
        /// photo upload operation.
        /// </summary>
        /// <param name="fileName">The name of the file that failed validation. This value is used to identify the file in error reporting.</param>
        /// <param name="errors">A collection of validation errors encountered during the upload process.</param>
        /// <returns>An <see cref="UploadPhotosResult"/> instance indicating a validation failure.</returns>
        public static UploadPhotosResult CreateValidationFailure(
            string fileName,
            IReadOnlyDictionary<string, string[]> errors)
            => new(
                blobName: string.Empty,
                fileName: fileName,
                trackingId: string.Empty,
                contentType: string.Empty)
            {
                Errors = [.. errors.SelectMany(kvp =>
                        kvp.Value.Select(msg => ExplicitErrors.ValidationError(kvp.Key, msg)))]
            };
    }
}