using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Services;
using MyBudgetIA.Infrastructure.Storage;
using MyBudgetIA.Infrastructure.Storage.Abstractions;

namespace MyBudgetIA.Infrastructure.Extensions
{
    /// <summary>
    /// Provides extension methods for registering application infrastructure services with an <see
    /// cref="IServiceCollection"/> for dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds infrastructure-related services and configuration to the specified service collection, including blob
        /// storage, photo, and validation services.
        /// </summary>
        /// <remarks>This method configures blob storage settings with validation to ensure either an
        /// account name or connection string is provided. It registers a singleton BlobServiceClient for Azure or local
        /// development, and adds scoped services for photo and validation operations. StreamValidation is performed at
        /// startup to fail fast if required settings are missing.</remarks>
        /// <param name="services">The service collection to which infrastructure services will be added. Must not be null.</param>
        /// <param name="configuration">The application configuration used to bind and validate infrastructure settings. Must not be null.</param>
        /// <returns>The same service collection instance, with infrastructure services and options registered.</returns>
        public static IServiceCollection
            AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<IPhotoService, PhotoService>();

            services.AddScoped<IValidationService, ValidationService>();

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

            return services;
        }
    }
}
