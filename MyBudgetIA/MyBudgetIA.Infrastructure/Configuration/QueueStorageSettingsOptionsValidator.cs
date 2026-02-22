using Microsoft.Extensions.Options;

namespace MyBudgetIA.Infrastructure.Configuration
{
    /// <summary>
    /// Validates configuration options for QueueStorage settings to ensure that required parameters are provided and
    /// correctly formatted.
    /// </summary>
    /// <remarks>This validator checks that either the AccountName or ConnectionString is specified, ensures
    /// that if ManagedIdentityClientId is used, the AccountName must also be provided, and verifies that the
    /// AccountName does not contain leading or trailing whitespace. It is intended to be used in conjunction with the
    /// options validation framework.</remarks>
    internal sealed class QueueStorageSettingsOptionsValidator : IValidateOptions<QueueStorageSettings>
    {
        /// <summary>
        /// Validates the specified QueueStorage settings and ensures that required parameters are configured correctly.
        /// </summary>
        /// <param name="name">The name of the queue to be validated. This parameter is optional and may be null.</param>
        /// <param name="options">The QueueStorageSettings object containing configuration options for the QueueStorage. This parameter cannot
        /// be null.</param>
        /// <returns>A ValidateOptionsResult indicating the success or failure of the validation, along with any relevant error
        /// messages.</returns>
        public ValidateOptionsResult Validate(string? name, QueueStorageSettings options)
        {
            if (options is null)
            {
                return ValidateOptionsResult.Fail("QueueStorage settings are missing.");
            }

            if (string.IsNullOrWhiteSpace(options.ConnectionString) && string.IsNullOrWhiteSpace(options.AccountName))
            {
                return ValidateOptionsResult.Fail(
                    "Either QueueStorage:AccountName or QueueStorage:ConnectionString must be configured");
            }

            if (!string.IsNullOrWhiteSpace(options.ManagedIdentityClientId) && string.IsNullOrWhiteSpace(options.AccountName))
            {
                return ValidateOptionsResult.Fail(
                    "QueueStorage:ManagedIdentityClientId requires QueueStorage:AccountName (ConnectionString mode does not use managed identity).");
            }

            if (!string.IsNullOrWhiteSpace(options.AccountName) && options.AccountName != options.AccountName.Trim())
            {
                return ValidateOptionsResult.Fail("QueueStorage:AccountName must not contain leading/trailing whitespace.");
            }

            // QueueName required is already enforced by DataAnnotations validation.
            return ValidateOptionsResult.Success;
        }
    }
}
