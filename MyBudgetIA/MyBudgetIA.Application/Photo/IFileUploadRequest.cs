namespace MyBudgetIA.Application.Photo
{
    /// <summary>
    /// Abstraction representing an uploaded file.
    /// </summary>
    public interface IFileUploadRequest
    {
        /// <summary>
        /// Gets the name of the file associated with this instance.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets the media type of the content represented by the instance.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Gets the length of the stream in bytes.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Opens the file for reading and returns a read-only stream to its contents.
        /// </summary>
        /// <returns>A read-only <see cref="Stream"/> for reading the file's contents. The caller is responsible for disposing
        /// the stream when finished.</returns>
        Stream OpenReadStream();

        /// <summary>
        /// Gets the file extension associated with the current item, including the leading period.
        /// </summary>
        string Extension { get; }
    }
}
