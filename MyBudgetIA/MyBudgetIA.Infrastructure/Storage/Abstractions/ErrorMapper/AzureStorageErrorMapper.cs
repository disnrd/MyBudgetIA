using Azure;
using MyBudgetIA.Infrastructure.Storage.Blob;

namespace MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper
{
    /// <inheritdoc cref="IAzureStorageErrorMapper"/>
    public class AzureStorageErrorMapper(IAzureStorageErrorMapperFactory factory) : IAzureStorageErrorMapper
    {
        /// <inheritdoc />
        public string Map(RequestFailedException ex, StorageOperationType operationType)
        {
            var mapper = factory.Get(operationType);
            return mapper.Map(ex, operationType);
        }
    }

}
