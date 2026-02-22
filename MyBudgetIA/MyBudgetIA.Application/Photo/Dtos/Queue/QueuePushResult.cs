using Shared.Models;

namespace MyBudgetIA.Application.Photo.Dtos.Queue
{
    /// <summary>
    /// Represents the result of a message sending operation, indicating whether the operation was successful and
    /// providing related metadata or error information.
    /// </summary>
    public sealed class QueuePushResult
    {
        /// <summary>
        /// Gets the date and time, in Coordinated Universal Time (UTC), when the entity was last modified.
        /// </summary>
        public DateTimeOffset? LastModifiedUtc { get; init; }

        /// <summary>
        /// Gets a value indicating whether the operation completed successfully.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Error message if message sending failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Error code if message sending failed (for programmatic handling).
        /// </summary>
        public string? ErrorCode { get; init; }

        /// <summary>
        /// Gets the unique identifier for the message associated with this instance.
        /// </summary>
        public string? MessageId { get; init; }

        /// <summary>
        /// Creates a successful message sending result.
        /// </summary>
        public static QueuePushResult CreateSuccess(
            string messageId,
            DateTimeOffset? lastModifiedUtc = null)
            => new()
            {
                LastModifiedUtc = lastModifiedUtc,
                IsSuccess = true,
                MessageId = messageId
            };

        /// <summary>
        /// Creates a failed message sending result.
        /// </summary>
        public static QueuePushResult CreateFailure(
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
