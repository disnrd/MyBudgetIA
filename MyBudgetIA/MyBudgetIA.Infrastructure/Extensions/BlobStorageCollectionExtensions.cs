using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using MyBudgetIA.Infrastructure.Storage.Blob;

namespace MyBudgetIA.Infrastructure.Extensions
{
    /// <summary>
    /// Provides extension methods for registering blob storage services with an application's dependency injection
    /// container.
    /// </summary>
    public static class BlobStorageCollectionExtensions
    {
        /// <summary>
        /// Adds blob storage services to the specified service collection using the provided configuration.
        /// </summary>
        /// <remarks>Call this method during application startup to enable blob storage functionality
        /// through dependency injection.</remarks>
        /// <param name="services">The service collection to which the blob storage services will be added. Cannot be null.</param>
        /// <param name="configuration">The configuration settings used to configure the blob storage services. Cannot be null.</param>
        /// <returns>The service collection with the blob storage services registered.</returns>
        public static IServiceCollection
            AddBlobStorage(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<BlobStorageSettings>()
               .Bind(configuration.GetSection(BlobStorageSettings.SectionName))
               .ValidateDataAnnotations()
               .ValidateOnStart(); // ← Fail-fast at startup

            services.AddSingleton<IValidateOptions<BlobStorageSettings>, BlobStorageSettingsOptionsValidator>();

            // Register BlobServiceClient (singleton)
            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<BlobStorageSettings>>().Value;

                // Mode 1: ConnectionString (Azurite local development)
                if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    return new BlobServiceClient(settings.ConnectionString);
                }

                // Mode 2: DefaultAzureCredential (Azure production)
                var blobUri = new Uri($"https://{settings.AccountName}{AzureConstants.BlobStorageEndpointSuffix}");
                var credential = string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId)
                    ? new DefaultAzureCredential()
                    : new DefaultAzureCredential
                        (new DefaultAzureCredentialOptions
                        {
                            ManagedIdentityClientId = settings.ManagedIdentityClientId
                        });

                return new BlobServiceClient(blobUri, credential);
            });

            services.AddSingleton<IAzureBlobServiceClient>(sp =>
                new AzureBlobServiceClientAdapter(sp.GetRequiredService<BlobServiceClient>()));

            services.AddScoped<IBlobStorageService, BlobStorageService>();

            services.AddSingleton<AzureBlobErrorMapper>();

            return services;
        }
    }
}
