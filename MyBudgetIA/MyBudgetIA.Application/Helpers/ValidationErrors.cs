using MyBudgetIA.Application.Exceptions;

namespace MyBudgetIA.Application.Helpers
{
    /// <summary>
    /// Helper to build validation errors (field -> messages) then throw an <see cref="ValidationException"/>.
    /// </summary>
    public sealed class ValidationErrors
    {
        private readonly Dictionary<string, List<string>> errors = new(StringComparer.Ordinal);

        /// <summary>
        /// Adds an error message associated with the specified field.
        /// </summary>
        /// <param name="field">The name of the field to associate with the error message. Cannot be null.</param>
        /// <param name="message">The error message to add for the specified field. Cannot be null.</param>
        public void Add(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list))
            {
                list = [];
                errors[field] = list;
            }

            list.Add(message);
        }

        /// <summary>
        /// Throws a ValidationException if any validation errors are present.
        /// </summary>
        /// <exception cref="ValidationException">Thrown when one or more validation errors have been collected.</exception>
        public void ThrowIfAny()
        {
            if (errors.Count == 0) return;

            throw new ValidationException(
                errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.Ordinal));
        }
    }
}
