using System.ComponentModel.DataAnnotations;

namespace MyBudgetIA.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration settings for Azure Queue Storage.
    /// </summary>
    public sealed class QueueStorageSettings
    {
        /// <summary>
        /// Gets the name of the configuration section used to identify the queue storage settings.
        /// </summary>
        public static string SectionName => "QueueStorage";

        /// <summary>
        /// Optional: Connection string for local development with Azurite.
        /// If provided, takes precedence over AccountName + DefaultAzureCredential.
        /// </summary>
        public string? ConnectionString { get; init; }

        /// <summary>
        /// Storage Account name (e.g., "stmybudgetia").
        /// Used with DefaultAzureCredential for Azure deployments.
        /// </summary>
        public string AccountName { get; init; } = string.Empty;

        /// <summary>
        /// Optional: client ID for user-assigned managed identity.
        /// </summary>
        public string? ManagedIdentityClientId { get; init; } = string.Empty;

        /// <summary>
        /// Queue name (e.g. "photo-ocr-request").
        /// </summary>
        [Required(ErrorMessage = "QueueStorage:QueueName is required")]
        [MinLength(3, ErrorMessage = "QueueName must be at least 3 characters")]
        public string QueueName { get; init; } = string.Empty;
    }
}
