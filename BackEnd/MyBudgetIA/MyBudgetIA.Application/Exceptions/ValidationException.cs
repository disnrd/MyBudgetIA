using Shared.Models;

namespace MyBudgetIA.Application.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when one or more validation errors occur during an operation.
    /// </summary>
    /// <remarks>Use this exception to indicate that input data failed validation and to provide detailed
    /// error information for each invalid field. The exception includes a collection of errors that can be used to
    /// display or log validation issues. This type is typically used in scenarios such as model validation in APIs or
    /// form processing.</remarks>
    /// <param name="errors">A read-only dictionary containing validation errors, where each key is the name of a field or property and the
    /// value is an array of error messages associated with that field. Cannot be null.</param>
    public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors) : ApplicationException(DefaultMessage, ErrorCodes.ValidationError, 400)
    {
        /// <summary>
        /// Gets a collection of validation errors, grouped by field or property name.
        /// </summary>
        /// <remarks>Each key in the dictionary represents the name of a field or property, and the
        /// associated value is an array of error messages for that field. The collection is read-only and will be empty
        /// if there are no validation errors.</remarks>
        public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

        private const string DefaultMessage = "One or more validation errors occurred.";
    }

}
