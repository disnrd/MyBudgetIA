using MyBudgetIA.Infrastructure.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace MyBudgetIA.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration settings for Azure Blob Storage.
    /// </summary>
    public sealed class BlobStorageSettings
    {
        /// <summary>
        /// Represents the configuration section name for blob storage settings.
        /// </summary>
        public static string SectionName => "BlobStorage";

        /// <summary>
        /// Storage Account name (e.g., "stmybudgetia").
        /// Used with DefaultAzureCredential for Azure deployments.
        /// </summary>
        public string AccountName { get; init; } = string.Empty;

        /// <summary>
        /// Blob container name (e.g., "photos").
        /// </summary>
        [Required(ErrorMessage = "BlobStorage:ContainerName is required")]
        [MinLength(3, ErrorMessage = "ContainerName must be at least 3 characters")]
        public string ContainerName { get; init; } = string.Empty;

        /// <summary>
        /// Optional: Connection string for local development with Azurite.
        /// If provided, takes precedence over AccountName + DefaultAzureCredential.
        /// </summary>
        public string? ConnectionString { get; init; }

        /// <summary>
        /// Optional: Gets the client ID to use when authenticating with a managed identity.
        /// </summary>
        /// <remarks>Specify this value to use a user-assigned managed identity instead of the
        /// system-assigned identity. If not set or empty, the default system-assigned managed identity is
        /// used.</remarks>
        public string? ManagedIdentityClientId { get; init; } = string.Empty;

        //public void Validate()
        //{
        //    if (!IsValid(out var errorMessage))
        //    {
        //        throw new InfrastructureConfigurationException(errorMessage);
        //    }
        //}

        /// <summary>
        /// Determines whether the current blob storage configuration is valid.
        /// </summary>
        /// <param name="errorMessage">When this method returns, contains an error message describing the first validation failure if the
        /// configuration is invalid; otherwise, an empty string.</param>
        /// <returns>true if the configuration is valid; otherwise, false.</returns>
        public bool IsValid(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString) && string.IsNullOrWhiteSpace(AccountName))
            {
                errorMessage = "Either BlobStorage:AccountName or BlobStorage:ConnectionString must be configured";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ManagedIdentityClientId) && string.IsNullOrWhiteSpace(AccountName))
            {
                errorMessage = "BlobStorage:ManagedIdentityClientId requires BlobStorage:AccountName (ConnectionString mode does not use managed identity).";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(AccountName) && AccountName != AccountName.Trim())
            {
                errorMessage = "BlobStorage:AccountName must not contain leading/trailing whitespace.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContainerName))
            {
                errorMessage = "BlobStorage:ContainerName is required";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        // TODO sortir les messages dans constantes pour unit tests
    }
}
