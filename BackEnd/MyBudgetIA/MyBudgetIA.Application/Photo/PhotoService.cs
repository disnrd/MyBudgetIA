using MyBudgetIA.Application.Photo.Dtos;

namespace MyBudgetIA.Application.Photo
{
    /// <inheritdoc cref="IPhotoService"/>
    public class PhotoService : IPhotoService
    {
        /// <summary>
        /// Asynchronously uploads a collection of photos.
        /// </summary>
        /// <param name="photos">A read-only collection of photo upload data to be uploaded. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous upload operation.</returns>
        /// <exception cref="NotImplementedException">The method is not implemented.</exception>
        public Task UploadAsync(IReadOnlyCollection<PhotoUploadDto> photos)
        {
            throw new NotImplementedException();
        }
    }
}
