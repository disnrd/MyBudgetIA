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

            return options.IsValid(out var error)
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(error);
        }
    }
}
