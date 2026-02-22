namespace MyBudgetIA.Infrastructure.Storage.Blob
{
    internal static class BlobMetadata
    {
        private static class Keys
        {
            public const string FileName = "FileName";
            public const string TrackingId = "TrackingId";
        }

        public static Dictionary<string, string> Create(string fileName, string trackingId)
            => new(StringComparer.Ordinal)
            {
                [Keys.FileName] = fileName,
                [Keys.TrackingId] = trackingId
            };
    }
}
