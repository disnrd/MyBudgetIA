namespace Shared.Exceptions
{
    /// <summary>
    /// Represents a temporary technical failure caused by an external dependency,
    /// such as storage, network, or external services. This exception is considered
    /// recoverable, and callers are expected to retry the operation. When thrown
    /// from an Azure Function, it will cause the message to be retried automatically.
    /// </summary>

    public sealed class TransientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the TransientException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error. This message provides additional context about the exception.</param>
        public TransientException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the TransientException class with a specified error message and a reference to
        /// the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
        public TransientException(string message, Exception inner) : base(message, inner) { }
    }
}
