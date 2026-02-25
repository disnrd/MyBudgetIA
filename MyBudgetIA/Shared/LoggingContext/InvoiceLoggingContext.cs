namespace Shared.LoggingContext
{
    /// <summary>
    /// Provides contextual information for logging invoice-related operations, including identifiers and message
    /// processing details.
    /// </summary>
    /// <param name="trackingId">The unique identifier used to track the invoice operation across systems.</param>
    /// <param name="blobName">The name of the blob associated with the invoice data.</param>
    /// <param name="messageId">The identifier of the message related to the invoice processing event.</param>
    /// <param name="dequeueCount">The number of times the message has been dequeued for processing.</param>
    public sealed class InvoiceLoggingContext(string trackingId, string blobName, string messageId, long dequeueCount)
    {
        public string TrackingId { get; } = trackingId;
        public string BlobName { get; } = blobName;
        public string MessageId { get; } = messageId;
        public long DequeueCount { get; } = dequeueCount;

        public Dictionary<string, object> ToScope() =>
            new()
            {
                ["TrackingId"] = TrackingId,
                ["BlobName"] = BlobName,
                ["MessageId"] = MessageId,
                ["DequeueCount"] = DequeueCount
            };
    }

}
