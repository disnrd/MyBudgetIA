using Shared.Models;

namespace MyBudgetIA.Application.Photo.Dtos
{
    /// <summary>
    /// Provides static methods for creating standardized validation error instances used to indicate issues with user
    /// input or data validation.
    /// </summary>
    /// <remarks>Use this class to generate validation errors that include a consistent error code and a
    /// descriptive message for the field that failed validation. This helps ensure uniform error handling and messaging
    /// throughout the application.</remarks>
    public static class ExplicitErrors
    {
        /// <summary>
        /// Creates a new ExplicitError instance for a specified field and error message.
        /// </summary>
        /// <param name="field">The name of the field that caused the validation error. Cannot be null or empty.</param>
        /// <param name="message">The error message that describes the validation issue. Cannot be null or empty.</param>
        /// <returns>A ValidationError object containing the specified field and error message.</returns>
        public static ExplicitError ValidationError(string field, string message) =>
            new()
            {
                Code = ErrorCodes.ValidationError,
                Field = field,
                Message = message
            };
    }

    /// <summary>
    /// Represents a validation error that provides details about a specific issue encountered during data validation.
    /// </summary>
    public class ExplicitError
    {
        /// <summary>
        /// Gets or sets the code that uniquely identifies the entity.
        /// </summary>
        public string Code { get; set; } = default!;

        /// <summary>
        /// Gets or sets the name of the field associated with the validation error.
        /// </summary>
        public string Field { get; set; } = default!;

        /// <summary>
        /// Gets or sets the message associated with the current instance.
        /// </summary>
        public string Message { get; set; } = default!;
    }
}