namespace MyBudgetIA.Infrastructure.Exceptions
{
    /// <summary>
    /// Base class for all infrastructure-related exceptions (Azure, external services, etc.).
    /// </summary>
    public abstract class InfrastructureException : Exception
    {
        /// <summary>
        /// Gets the error code that identifies the specific error condition.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Gets the HTTP status code (default: 500 for infrastructure errors).
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the public message associated with the current instance.
        /// </summary>
        public string PublicMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfrastructureException"/> class.
        /// </summary>
        protected InfrastructureException(
            string message,
            string errorCode,
            int statusCode = 500)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
            PublicMessage = message;
        }

        /// <summary>
        /// Initializes a new instance with an inner exception.
        /// </summary>
        protected InfrastructureException(
            string message,
            string errorCode,
            int statusCode,
            Exception? innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
            PublicMessage = message;
        }

        /// <summary>
        /// Initializes a new instance with a distinct public message and an optional inner exception.
        /// </summary>
        protected InfrastructureException(
            string publicMessage,
            string errorCode,
            int statusCode,
            string? internalMessage,
            Exception? innerException)
            : base(internalMessage ?? publicMessage, innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
            PublicMessage = publicMessage;
        }
    }
}
