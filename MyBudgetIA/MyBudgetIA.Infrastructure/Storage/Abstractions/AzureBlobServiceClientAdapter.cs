using Azure.Storage.Blobs;

namespace MyBudgetIA.Infrastructure.Storage.Abstractions
{

    /// <summary>
    /// Provides an adapter for the Azure Blob Storage service client, enabling interaction with blob containers through
    /// the IAzureBlobServiceClient interface, to facilitate abstraction and potential mocking in unit tests.
    /// This class wraps an instance of the BlobServiceClient
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the AzureBlobServiceClientAdapter class using the specified BlobServiceClient.
    /// </remarks>
    /// <param name="blobServiceClient">The BlobServiceClient instance to use for Azure Blob Storage operations. Cannot be null.</param>
    public class AzureBlobServiceClientAdapter(BlobServiceClient blobServiceClient) : IAzureBlobServiceClient
    {
        /// <summary>
        /// Gets a client object for interacting with the specified blob container.
        /// </summary>
        /// <param name="containerName">The name of the blob container to connect to. Cannot be null or empty.</param>
        /// <returns>A BlobContainerClient instance for the specified container.</returns>
        public BlobContainerClient GetBlobContainerClient(string containerName)
        {
            return blobServiceClient.GetBlobContainerClient(containerName);
        }
    }
}
