namespace Shared.Storage.DTOS
{
    /// <summary>
    /// Represents a request to enqueue a message containing metadata for processing in a queue-based workflow.
    /// </summary>
    /// <param name="BlobName">The name of the blob associated with the message, used for storage and retrieval operations.</param>
    /// <param name="TrackingId">The unique identifier used to track the message throughout its processing lifecycle.</param>
    /// <param name="SchemaVersion">The version of the message schema. Defaults to 1 if not specified.</param>
    public sealed record QueueMessageRequest(
        string BlobName,
        string TrackingId,
        int SchemaVersion = 1);
}
