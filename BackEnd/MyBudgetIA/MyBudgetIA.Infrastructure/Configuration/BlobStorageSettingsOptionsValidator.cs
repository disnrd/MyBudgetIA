using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace MyBudgetIA.Infrastructure.Configuration
{
    internal sealed class BlobStorageSettingsOptionsValidator : IValidateOptions<BlobStorageSettings>
    {
        public ValidateOptionsResult Validate(string? name, BlobStorageSettings options)
        {
            if (options is null)
            {
                return ValidateOptionsResult.Fail("BlobStorage settings are null.");
            }

            if (!options.IsValid(out var error))
            {
                return ValidateOptionsResult.Fail(error);
            }

            // Fail-fast: validate connection string format (dev/Azurite mode).
            // BlobServiceClient ctor will throw if the connection string is malformed.
            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                try
                {
                    _ = new BlobServiceClient(options.ConnectionString);
                }
                catch (Exception ex) when (ex is FormatException or ArgumentException)
                {
                    return ValidateOptionsResult.Fail(
                        $"BlobStorage:ConnectionString is invalid. {ex.Message}");
                }
            }

            return ValidateOptionsResult.Success;
        }
    }
}
