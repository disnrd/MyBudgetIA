namespace MyBudgetIA.Application.Exceptions
{
    /// <summary>
    /// Represents an application-level exception that includes a public-facing message, an error code, and an HTTP
    /// status code for API error handling.
    /// </summary>
    /// <remarks>This exception type is designed for use in API scenarios where a clear, client-facing error
    /// message and structured error information are required. The <see cref="PublicMessage"/> property provides a
    /// stable message for clients, which may differ from the standard <see cref="Exception.Message"/>. The <see
    /// cref="ErrorCode"/> and <see cref="StatusCode"/> properties facilitate consistent error handling and response
    /// formatting.</remarks>
    /// <param name="publicMessage">The message intended for clients or API consumers. This message should be stable and suitable for exposure in
    /// API responses.</param>
    /// <param name="errorCode">A code that uniquely identifies the specific error condition. Used for programmatic error handling and
    /// diagnostics.</param>
    /// <param name="statusCode">The HTTP status code to associate with the error response. Defaults to 500 if not specified.</param>
    /// <param name="internalMessage">An optional internal message for logging or diagnostics. If not provided, <paramref name="publicMessage"/> is
    /// used.</param>
    /// <param name="innerException">The underlying exception that caused this exception, if any. Used to provide additional error context.</param>
    public abstract class ApplicationException(
        string publicMessage,
        string errorCode,
        int statusCode = 500,
        string? internalMessage = null,
        Exception? innerException = null) : Exception(internalMessage ?? publicMessage, innerException)
    {

        /// <summary>
        /// Gets the error code that identifies the specific error condition encountered.
        /// </summary>
        public string ErrorCode { get; } = errorCode;

        /// <summary>
        /// Gets the HTTP status code returned by the response.
        /// </summary>
        public int StatusCode { get; } = statusCode;

        /// <summary>
        /// Message for the client.
        /// </summary>
        public string PublicMessage { get; } = publicMessage;
    }
}
