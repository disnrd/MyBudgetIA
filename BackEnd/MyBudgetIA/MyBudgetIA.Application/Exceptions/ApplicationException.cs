namespace MyBudgetIA.Application.Exceptions
{
    /// <summary>
    /// Represents the base class for application-specific exceptions that include an error code and HTTP status code.
    /// </summary>
    /// <remarks>Inherit from this class to create custom exceptions that provide additional error
    /// information, such as a unique error code and an associated HTTP status code. This facilitates consistent error
    /// handling and reporting across the application.</remarks>
    /// <remarks>
    /// Initializes a new instance of the ApplicationException class with a specified error message, error code, and
    /// optional status code.
    /// </remarks>
    /// <remarks>Use this constructor to provide additional error context, such as a custom error code
    /// and status code, when throwing application-specific exceptions.</remarks>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">A string that uniquely identifies the type or category of error.</param>
    /// <param name="statusCode">The HTTP status code associated with the error. The default is 500.</param>
    public abstract class ApplicationException(string message, string errorCode, int statusCode = 500) : Exception(message)
    {
        /// <summary>
        /// Gets the error code that identifies the specific error condition encountered.
        /// </summary>
        public string ErrorCode { get; } = errorCode;

        /// <summary>
        /// Gets the HTTP status code returned by the response.
        /// </summary>
        public int StatusCode { get; } = statusCode;
    }
}
