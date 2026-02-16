using MyBudgetIA.Application.Photo.Dtos;
using Shared.Storage.DTOS;

namespace MyBudgetIA.Application.Interfaces
{
    /// <summary>
    /// Technical services defining a contract for interacting with a blob storage service.
    /// </summary>
    /// <remarks>Implementations of this interface provide methods for storing, retrieving, and managing
    /// binary large objects (blobs) in a storage system such as cloud or on-premises storage. The specific operations
    /// and behaviors are defined by the implementing class.</remarks>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Asynchronously uploads a file to the blob storage using the specified upload request.
        /// </summary>
        /// <remarks>If the cancellation token is triggered before the upload completes, the operation is
        /// canceled and the returned task will be in a canceled state.</remarks>
        /// <param name="request">The details of the file to upload.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the upload operation.</param>
        /// <returns>A task that represents the asynchronous upload operation.</returns>
        Task<BlobUploadResult> UploadFileAsync(BlobUploadRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads the specified blob from the container asynchronously.
        /// </summary>
        /// <param name="blobName">The name of the blob to download. This must be a valid blob name within the container.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation if needed.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a BlobDownloadData object, which
        /// includes the content stream, content type, content length, file name, tracking ID, and metadata associated
        /// with the downloaded blob.</returns>
        Task<BlobDownloadData> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a collection of blob data asynchronously from the storage service.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation if needed.</param>
        /// <remarks>This method use an application Dtos and will not be consumed in a futur worker.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of <see
        /// cref="BlobData"/> objects representing the blobs retrieved. The collection will be empty if no blobs are
        /// found.</returns>
        Task<IEnumerable<BlobData>> GetBlobsInfoAsync(CancellationToken cancellationToken = default);
    }
}
