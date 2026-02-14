using Azure.Storage.Blobs;

namespace MyBudgetIA.Infrastructure.Storage.Abstractions
{

    /// <summary>
    /// Defines a client interface for accessing Azure Blob Service methods.
    /// </summary>
    /// <remarks>Implementations of this interface provide methods to obtain clients for specific blob
    /// containers within an Azure Storage account. This interface is intended to abstract the details of connecting to
    /// Azure Blob Storage, enabling easier testing and dependency injection.</remarks>
    public interface IAzureBlobServiceClient
    {
        /// <summary>
        /// Gets a client object for interacting with the specified blob container.
        /// </summary>
        /// <param name="containerName">The name of the blob container to connect to. Cannot be null or empty.</param>
        /// <returns>A <see cref="BlobContainerClient"/> instance for the specified container.</returns>
        BlobContainerClient GetBlobContainerClient(string containerName);
    }
}
