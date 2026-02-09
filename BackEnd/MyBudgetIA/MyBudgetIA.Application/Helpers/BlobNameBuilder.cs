namespace MyBudgetIA.Application.Helpers
{
    internal static class BlobNameBuilder
    {
        public static string GenerateUniqueBlobName(
            string prefix, 
            string fileName,
            string trackingId)
        {
            string sanitized = AzureNameSanitizer.SanitizeBlobName(fileName);
            string uploadedAt = DateTime.UtcNow.ToString("yyyyMMdd");

            return $"{prefix}/{uploadedAt}/{trackingId}-{sanitized}";
        }
    }
}