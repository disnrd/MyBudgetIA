using Microsoft.Extensions.DependencyInjection;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Infrastructure.Services;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;

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
        /// <returns>The same service collection instance, with infrastructure services and options registered.</returns>
        public static IServiceCollection
            AddInfrastructure(
            this IServiceCollection services)
        {
            services.AddScoped<IPhotoService, PhotoService>();

            services.AddScoped<IValidationService, ValidationService>();

            services.AddSingleton<IAzureStorageErrorMapperFactory, AzureStorageErrorMapperFactory>();
            services.AddSingleton<IAzureStorageErrorMapper, AzureStorageErrorMapper>();

            return services;
        }
    }
}
