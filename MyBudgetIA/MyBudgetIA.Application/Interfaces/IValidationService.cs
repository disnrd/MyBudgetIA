namespace MyBudgetIA.Application.Interfaces
{
    /// <summary>
    /// Defines a service for validating objects and retrieving validation results asynchronously.
    /// </summary>
    /// <remarks>Implementations of this interface provide methods to validate objects of any type and to
    /// retrieve validation errors, if any. These methods are typically used to enforce business rules or data integrity
    /// before processing or persisting objects. The interface supports asynchronous validation and cancellation via a
    /// cancellation token.</remarks>
    public interface IValidationService
    {
        /// <summary>
        /// Asynchronously validates the specified instance and throws an exception if validation fails.
        /// </summary>
        /// <remarks>If validation fails, an exception is thrown containing details about the validation
        /// errors. This method is typically used to enforce validation rules before processing or persisting
        /// data.</remarks>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="instance">The object instance to validate. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the validation operation.</param>
        /// <returns>A task that represents the asynchronous validation operation. The task completes successfully if validation
        /// passes; otherwise, it faults with a validation exception.</returns>
        Task ValidateAndThrowAsync<T>(T instance, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously validates the specified instance and throws an exception if validation fails.
        /// This methods call all related validators to gather all validation errors.
        /// </summary>
        /// <remarks>If validation fails, an exception is thrown containing all validation errors. The
        /// method does not return a result; it completes successfully only if the instance passes all validation
        /// rules.</remarks>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="instance">The object to validate. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the validation operation.</param>
        /// <returns>A task that represents the asynchronous validation operation.</returns>
        Task ValidateAndThrowAllAsync<T>(T instance, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously attempts to validate the specified instance and returns a result indicating whether
        /// validation succeeded, along with any validation errors.
        /// </summary>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="instance">The object instance to validate. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the validation operation.</param>
        /// <returns>A task that represents the asynchronous operation. The result contains a boolean indicating whether the
        /// instance is valid, and a read-only dictionary of validation errors keyed by property name. If validation
        /// succeeds, the dictionary will be empty.</returns>
        Task<(bool IsValid, IReadOnlyDictionary<string, string[]> Errors)> TryValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
    }
}
