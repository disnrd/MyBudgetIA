using MyBudgetIA.Application.Photo;

namespace MyBudgetIA.Api.Uploads
{
    internal sealed class FormFileAdapter(IFormFile file) : IFileUploadRequest
    {
        public string FileName => file.FileName ?? string.Empty;
        public string ContentType => file.ContentType ?? string.Empty;
        public long Length => file.Length;
        public Stream OpenReadStream() => file.OpenReadStream();
        public string Extension => Path.GetExtension(file.FileName ?? string.Empty);
    }
}
