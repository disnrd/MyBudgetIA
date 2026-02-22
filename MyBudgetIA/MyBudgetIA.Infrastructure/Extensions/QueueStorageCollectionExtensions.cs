using Azure.Core;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using MyBudgetIA.Infrastructure.Storage.Queue;

namespace MyBudgetIA.Infrastructure.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring services related to queue storage in a service collection.
    /// </summary>
    /// <remarks>This class contains methods that facilitate the addition of queue storage services to the
    /// application's dependency injection container, ensuring proper configuration and validation of
    /// settings.</remarks>
    public static class QueueStorageCollectionExtensions
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
            AddQueueStorage(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<QueueStorageSettings>()
                .Bind(configuration.GetSection(QueueStorageSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<IValidateOptions<QueueStorageSettings>, QueueStorageSettingsOptionsValidator>();

            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<QueueStorageSettings>>().Value;

                // retry policy to improve resilience
                var clientOptions = new QueueClientOptions
                {
                    Retry =
                    {
                        Mode = RetryMode.Exponential,
                        MaxRetries = 3,
                        Delay = TimeSpan.FromMilliseconds(250),
                        MaxDelay = TimeSpan.FromSeconds(5),
                    }
                };

                // Mode 1: ConnectionString (Azurite local development)
                if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    return new QueueServiceClient(settings.ConnectionString, clientOptions);
                }


                // Mode 2: DefaultAzureCredential (Azure production)
                var queueUri = new Uri($"https://{settings.AccountName}{AzureConstants.QueueStorageEndpointSuffix}");
                var credential = string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId)
                    ? new DefaultAzureCredential()
                    : new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = settings.ManagedIdentityClientId
                    });

                return new QueueServiceClient(queueUri, credential, clientOptions);
            });

            // QueueClient for the configured queue
            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<QueueStorageSettings>>().Value;
                var serviceClient = sp.GetRequiredService<QueueServiceClient>();

                return serviceClient.GetQueueClient(settings.QueueName);
            });

            services.AddSingleton<AzureQueueErrorMapper>();

            services.AddSingleton<IAzureQueueServiceClient>(sp =>
                new AzureQueueServiceClientAdapter(sp.GetRequiredService<QueueClient>()));

            services.AddScoped<IQueueStorageService, QueueStorageService>();

            return services;
        }
    }
}
