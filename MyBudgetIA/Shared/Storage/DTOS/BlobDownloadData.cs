namespace Shared.Storage.DTOS
{
    public class BlobDownloadData
    {
        public required Stream Content { get; init; }
        public string ContentType { get; init; } = "application/octet-stream";
        public long ContentLength { get; init; }
        public string? FileName { get; init; }
        public string? TrackingId { get; init; }
        public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    }
}
