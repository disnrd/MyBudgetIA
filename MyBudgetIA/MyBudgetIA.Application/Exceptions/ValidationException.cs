using Shared.Models;

namespace MyBudgetIA.Application.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when one or more validation errors occur during an operation.
    /// </summary>
    public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : ApplicationException(
            publicMessage: DefaultMessage,
            errorCode: ErrorCodes.ValidationError,
            statusCode: 400,
            internalMessage: BuildInternalMessage(errors))
    {
        /// <summary>
        /// Gets a collection of validation errors, grouped by field or property name.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

        private const string DefaultMessage = "One or more validation errors occurred.";

        private static string BuildInternalMessage(IReadOnlyDictionary<string, string[]> errors)
        {
            if (errors.Count == 0) return DefaultMessage;

            var details = errors.Select(kvp =>
                $"{kvp.Key}: {string.Join(" | ", kvp.Value.Where(v => !string.IsNullOrWhiteSpace(v)))}");

            return $"{DefaultMessage} Details: {string.Join("; ", details)}";
        }
    }
}
