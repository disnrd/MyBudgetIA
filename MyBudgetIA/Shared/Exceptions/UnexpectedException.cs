namespace Shared.Exceptions
{
    /// <summary>
    /// Represents an unexpected or unclassified error that does not fall under
    /// functional or transient categories. This typically indicates a bug or an
    /// inconsistent application state. The exception should be logged as critical,
    /// and callers are expected to retry the operation to allow transient issues
    /// to resolve or to surface the underlying defect.
    /// </summary>
    public sealed class UnexpectedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the UnexpectedException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error. This message is intended to provide a clear explanation of the
        /// exception's cause.</param>
        public UnexpectedException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the UnexpectedException class with a specified error message and a reference
        /// to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is
        /// specified.</param>
        public UnexpectedException(string message, Exception inner) : base(message, inner) { }
    }
}
