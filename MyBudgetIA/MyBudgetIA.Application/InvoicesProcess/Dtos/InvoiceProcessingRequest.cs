namespace MyBudgetIA.Application.InvoicesProcess.Dtos
{
    /// <summary>
    /// Represents a request to process an invoice, including tracking information and the associated blob name.
    /// </summary>
    /// <param name="trackingId">The unique identifier used to track the operation or request.</param>
    /// <param name="blobName">The name of the blob associated with the invoice processing request.</param>
    public sealed class InvoiceProcessingRequest(string trackingId, string blobName)
    {
        /// <summary>
        /// Gets the unique identifier used to track the associated operation or request.
        /// </summary>
        public string TrackingId { get; } = trackingId;

        /// <summary>
        /// Gets the name of the blob associated with this instance.
        /// </summary>
        public string BlobName { get; } = blobName;
    }
}