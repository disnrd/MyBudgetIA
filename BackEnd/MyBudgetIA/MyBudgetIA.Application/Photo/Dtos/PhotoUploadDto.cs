namespace MyBudgetIA.Application.Photo.Dtos
{
    /// <summary>
    /// Represents a photo file to be uploaded, including its metadata and content stream.
    /// </summary>
    /// <param name="FileName">The name of the file, including the file extension. Cannot be null or empty.</param>
    /// <param name="ContentType">The MIME type of the file, such as "image/jpeg" or "image/png". Cannot be null or empty.</param>
    /// <param name="Length">The length of the file content, in bytes. Must be zero or greater.</param>
    /// <param name="Content">A stream containing the file's binary content. The stream must be readable and positioned at the start of the
    /// file data. Cannot be null.</param>
    /// <param name="Extension">The file extension, including the leading period (Allowed: ".jpg", ".png", "jpeg", ".webp", ".heic"). Cannot be null or empty.</param>
    public record PhotoUploadDto(
        string FileName,
        string ContentType,
        long Length,
        Stream Content,
        string Extension);
}
