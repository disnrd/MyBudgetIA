namespace MyBudgetIA.Application.Photo.Dtos.Blob
{
    /// <summary>
    /// Represents a request to upload a blob.
    /// </summary>
    /// <param name="fileName">The original file name of the uploaded file.</param>
    /// <param name="blobName">The name of the blob to be uploaded. Cannot be null or empty.</param>
    /// <param name="stream">The stream containing the data to be uploaded. Must be readable 
    /// and positioned at the start of the content to
    /// upload.</param>
    /// <param name="contentType">The media type of the content associated with the request, such as "image/png" or "application/pdf".</param>
    /// <param name="trackingId">The trackingId identifier used to track and associate related operations or requests. If not provided, a new GUID will be generated.</param>
    public class BlobUploadRequest(string fileName, string blobName, Stream stream, string contentType, string trackingId)
    {
        /// <summary>
        /// Gets or sets the name of the file associated with this instance.
        /// </summary>
        public string FileName { get; set; } = fileName;

        /// <summary>
        /// Gets or sets the name of the file associated with this instance.
        /// </summary>
        public string BlobName { get; set; } = blobName;

        /// <summary>
        /// /// Gets or sets the stream containing the data to be uploaded.
        /// </summary>
        public Stream Stream { get; set; } = stream;

        /// <summary>
        /// Gets or sets the media type of the content associated with the request.
        /// </summary>
        public string ContentType { get; set; } = contentType;

        /// <summary>
        /// Gets or sets the trackingId identifier used to track and associate related operations or requests.
        /// </summary>
        public string TrackingId { get; set; } = trackingId;
    }
}
