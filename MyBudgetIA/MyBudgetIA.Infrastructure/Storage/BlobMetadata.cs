namespace MyBudgetIA.Infrastructure.Storage
{
    internal static class BlobMetadata
    {
        private static class Keys
        {
            public const string FileName = "fileName";
            public const string TrackingId = "trackingId";
        }

        public static Dictionary<string, string> Create(string fileName, string trackingId)
            => new(StringComparer.Ordinal)
            {
                [Keys.FileName] = fileName,
                [Keys.TrackingId] = trackingId
            };
    }
}
