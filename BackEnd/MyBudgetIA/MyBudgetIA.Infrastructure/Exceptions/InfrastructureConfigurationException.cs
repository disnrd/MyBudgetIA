using Shared.Models;

namespace MyBudgetIA.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception thrown when an infrastructure configuration is invalid or missing.
    /// </summary>
    /// <remarks>
    /// Creates a new <see cref="InfrastructureConfigurationException"/>.
    /// </remarks>
    /// <param name="internalMessage">Detailed message for logs/diagnostics.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public sealed class InfrastructureConfigurationException(
        string internalMessage,
        Exception? innerException = null) : InfrastructureException(
            publicMessage: DefaultPublicMessage,
            errorCode: ErrorCodes.InternalError,
            statusCode: 500,
            internalMessage: internalMessage,
            innerException: innerException)
    {
        private const string DefaultPublicMessage = "An internal configuration error occurred.";
    }
}
