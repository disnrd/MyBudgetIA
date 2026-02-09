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
    }
}
