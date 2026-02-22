using Azure;
using MyBudgetIA.Infrastructure.Storage.Blob;

namespace MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper
{
    /// <summary>
    /// Provides a mechanism for mapping Azure RequestFailedException instances to user-friendly error messages based on
    /// the type of storage operation performed.
    /// </summary>
    /// <remarks>Implementations of this interface should translate Azure storage errors into meaningful
    /// messages that are appropriate for the context of the operation. This enables consistent and informative error
    /// handling across different storage operations. Consider the specific operation type and exception details when
    /// generating messages to ensure clarity for end users.</remarks>
    public interface IAzureStorageErrorMapper
    {
        /// <summary>
        /// Maps a storage-related exception to a user-friendly error message based on the specified storage operation
        /// type.
        /// </summary>
        /// <param name="ex">The exception that contains details about the failure encountered during a storage operation. Cannot be
        /// null.</param>
        /// <param name="operationType">The type of storage operation that was being performed when the exception occurred. Determines the context
        /// of the error message.</param>
        /// <returns>A string containing a user-friendly error message that describes the failure in the context of the specified
        /// operation.</returns>
        string Map(RequestFailedException ex, StorageOperationType operationType);
    }
}
