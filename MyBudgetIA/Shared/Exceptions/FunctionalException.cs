namespace Shared.Exceptions
{
    /// <summary>
    /// Represents a business rule violation or a domain-level inconsistency.
    /// This exception indicates that the operation cannot be completed because
    /// the input or state does not satisfy the domain rules. It is not recoverable
    /// and should not trigger retries. The caller is expected to treat this as a
    /// functional failure and stop processing the message.
    /// </summary>
    public sealed class FunctionalException : Exception 
    {
        public FunctionalException(string message) : base(message) { }
        public FunctionalException(string message, Exception inner) : base(message, inner) { }
    }
}
