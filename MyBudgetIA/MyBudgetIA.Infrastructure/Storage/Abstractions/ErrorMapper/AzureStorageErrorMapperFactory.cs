using MyBudgetIA.Infrastructure.Storage.Blob;
using MyBudgetIA.Infrastructure.Storage.Queue;

namespace MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper
{
    /// <inheritdoc cref="IAzureStorageErrorMapperFactory"/>
    public class AzureStorageErrorMapperFactory(
        AzureBlobErrorMapper blob,
        AzureQueueErrorMapper queue) : IAzureStorageErrorMapperFactory
    {
        /// <inheritdoc />
        public IAzureStorageErrorMapper Get(StorageOperationType operationType)
        {
            return operationType switch
            {
                StorageOperationType.BlobUpload or StorageOperationType.BlobDownload => blob,
                StorageOperationType.QueueMessageSending or StorageOperationType.QueueMessageReceiving => queue,
                _ => throw new NotSupportedException()
            };
        }
    }
}
