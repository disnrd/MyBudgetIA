using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Infrastructure.Services;

namespace MyBudgetIA.Infrastructure.Extensions
{
    /// <summary>
    /// Provides extension methods for registering application infrastructure services with an <see
    /// cref="IServiceCollection"/> for dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds application-level services to the specified service collection for dependency injection.
        /// </summary>
        /// <param name="services">The service collection. Must not be null.</param>
        /// <returns>The given <paramref name="services"/> to further configure Dependency Injection; never
        /// <see langword="null"/>.</returns>

        public static IServiceCollection AddInfrastructure(
                    this IServiceCollection services)
        {
            services.AddScoped<IPhotoService, PhotoService>();

            services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(); // <-- Registers all FluentValidation validators in the application assembly

            services.AddScoped<IValidationService, ValidationService>();

            return services;
        }
    }
}
