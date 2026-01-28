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
        /// <param name="photos">A IReadOnlyCollection of <see cref="PhotoUploadDto"/> representing the photos to upload.
        /// Cannot be null or empty or contain null elements.</param>
        /// <returns>A task that represents the asynchronous upload operation.</returns>
        Task UploadAsync(IReadOnlyCollection<PhotoUploadDto> photos);
    }
}
