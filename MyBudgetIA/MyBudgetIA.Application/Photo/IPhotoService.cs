using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Application.Photo.Dtos.Blob;

namespace MyBudgetIA.Application.Photo
{
    /// <summary>
    /// Provides functionality for managing and processing photos.
    /// </summary>
    public interface IPhotoService
    {
        /// <summary>
        /// Asynchronously uploads a collection of photos, then send an alert message to a queue and returns the results of the two operations.
        /// </summary>
        /// <param name="photos">A read-only collection of file upload requests representing the photos to upload. Each request must contain
        /// valid file data.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the upload operation. The default value is <see
        /// cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with an enumerable of
        /// upload results and a message indicating the outcome of the upload.</returns>
        Task<(IEnumerable<UploadPhotosResult> result, string message)> UploadPhotoAsync(
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
