namespace MyBudgetIA.Infrastructure.Configuration
{
    /// <summary>
    /// Provides constant values for Azure service endpoint suffixes used in resource connection strings and URIs.
    /// </summary>
    /// <remarks>This class contains well-known endpoint suffixes for various Azure services, including Blob
    /// Storage, Cosmos DB, Queue Storage, and Cognitive Services. These constants can be used to construct
    /// service-specific endpoints or to validate Azure resource URIs. The class is static and cannot be
    /// instantiated.</remarks>
    public static class AzureConstants
    {
        /// <summary>
        /// Represents the default endpoint suffix for Azure Blob Storage service URLs.
        /// </summary>
        public const string BlobStorageEndpointSuffix = ".blob.core.windows.net";
    }
}
