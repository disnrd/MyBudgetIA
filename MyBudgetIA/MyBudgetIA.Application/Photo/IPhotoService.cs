using MyBudgetIA.Application.Photo.Dtos;

namespace MyBudgetIA.Application.Photo
{
    /// <summary>
    /// Provides functionality for managing and processing photos.
    /// </summary>
    public interface IPhotoService
    {
        /// <summary>
        /// Asynchronously uploads a collection of photos.
        /// </summary>
        /// <param name="photos">A collection of files to upload (transport-agnostic).</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the upload operation.</param>
        /// <returns>
        /// A task that represents the asynchronous upload operation. The task result contains an
        /// <see cref="UploadPhotosResult"/> instance representing the overall outcome and the per-file results.
        /// </returns>
        Task<UploadPhotosResult> UploadPhotoAsync(
            IReadOnlyCollection<IFileUploadRequest> photos,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves the photo blob associated with the specified blob name.
        /// </summary>
        /// <param name="blobName">The name of the blob to retrieve. This parameter cannot be null or empty.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="DownloadedPhotoDto"/> object with
        /// the retrieved photo blob data.</returns>
        Task<DownloadedPhotoDto> DownloadPhotoAsync(string blobName, CancellationToken ct);

        /// <summary>
        /// Asynchronously retrieves metadata for all uploaded photos from the storage.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of BlobData
        /// objects, each representing metadata for an uploaded photo.</returns>
        Task<IEnumerable<BlobData>> GetUploadedPhotosInfosAsync(CancellationToken cancellationToken = default);
    }
}
