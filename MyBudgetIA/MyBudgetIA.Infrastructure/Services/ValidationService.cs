using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MyBudgetIA.Application.Interfaces;

namespace MyBudgetIA.Infrastructure.Services
{
    /// <inheritdoc cref="IValidationService"/>
    public sealed class ValidationService(IServiceProvider serviceProvider) : IValidationService
    {
        private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
            new Dictionary<string, string[]>();

        /// <inheritdoc/>
        public async Task ValidateAndThrowAsync<T>(
            T instance,
            CancellationToken cancellationToken = default)
        {
            var validator = serviceProvider.GetService<IValidator<T>>();

            if (validator is null) return;

            var result = await validator.ValidateAsync(instance, cancellationToken);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage)
                    .ToArray());

                throw new Application.Exceptions.ValidationException(errors);
            }
        }

        /// <inheritdoc/>
        public async Task<(bool IsValid, IReadOnlyDictionary<string, string[]> Errors)> TryValidateAsync<T>(
            T instance,
            CancellationToken cancellationToken = default)
        {
            var validator = serviceProvider.GetService<IValidator<T>>();

            if (validator is null) return (true, EmptyErrors);

            var result = await validator.ValidateAsync(instance, cancellationToken);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage)
                    .ToArray());

                return (false, errors);
            }

            return (true, EmptyErrors);
        }

        /// <inheritdoc/>
        public async Task ValidateAndThrowAllAsync<T>(
            T instance,
            CancellationToken cancellationToken = default)
        {
            var validators = serviceProvider.GetServices<IValidator<T>>().ToList();

            if (validators.Count == 0) return; var allErrors = new List<FluentValidation.Results.ValidationFailure>();
            
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(instance, cancellationToken);
                
                if (!result.IsValid) allErrors.AddRange(result.Errors);
            }
            
            if (allErrors.Count > 0)
            {
                var grouped = allErrors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                
                throw new Application.Exceptions.ValidationException(grouped);
            }
        }
    }
}