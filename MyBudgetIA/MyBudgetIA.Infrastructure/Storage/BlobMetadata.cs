namespace MyBudgetIA.Infrastructure.Storage
{
    internal static class BlobMetadata
    {
        private static class Keys
        {
            public const string CreatedAt = "createdAtUtc";
            public const string ContentType = "contentType";
            public const string TrackingId = "trackingId";
        }

        public static Dictionary<string, string> Create(string contentType, string trackingId, DateTime createdAt)
            => new(StringComparer.Ordinal)
            {
                [Keys.CreatedAt] = createdAt.ToString("O"),
                [Keys.ContentType] = contentType,
                [Keys.TrackingId] = trackingId
            };
    }
}
