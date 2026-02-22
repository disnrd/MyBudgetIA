using MyBudgetIA.Infrastructure.Storage.Blob;

namespace MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper
{
    /// <summary>
    /// Defines a factory for retrieving instances of <see cref="IAzureStorageErrorMapper"/> that are appropriate for a
    /// specified storage operation type.
    /// </summary>
    /// <remarks>Implementations of this interface provide error mapping logic tailored to different storage
    /// operations. Use this factory to obtain an error mapper that handles errors specific to the given <see
    /// cref="StorageOperationType"/>.</remarks>
    public interface IAzureStorageErrorMapperFactory 
    { 
        /// <summary>
        /// Retrieves an error mapper that is configured to handle errors for the specified storage operation type.
        /// </summary>
        /// <param name="operationType">The type of storage operation for which to obtain an error mapper. This value determines how errors are
        /// interpreted and mapped.</param>
        /// <returns>An instance of IAzureErrorMapper that provides error mapping logic appropriate for the given storage
        /// operation type.</returns>
        IAzureStorageErrorMapper Get(StorageOperationType operationType); }
}
