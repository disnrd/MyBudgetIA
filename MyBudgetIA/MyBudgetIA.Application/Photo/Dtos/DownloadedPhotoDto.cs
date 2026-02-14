namespace MyBudgetIA.Application.Photo.Dtos
{
    /// <summary>
    /// Represents a photo that has been downloaded, including its content stream, file name, and MIME content type.
    /// </summary>
    public sealed class DownloadedPhotoDto
    {
        /// <summary>
        /// Gets the content as a stream for reading or writing data.
        /// </summary>
        public required Stream Content { get; init; }

        /// <summary>
        /// Gets the name of the file associated with the current instance.
        /// </summary>
        public required string FileName { get; init; }

        /// <summary>
        /// Gets the content type of the associated resource, which specifies the media type of the content.
        /// </summary>
        public required string ContentType { get; init; }
    }
}
