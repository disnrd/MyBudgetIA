using Microsoft.AspNetCore.Mvc;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Application.Photo.Dtos;
using Shared.Models;

namespace MyBudgetIA.Api.Controllers
{
    public class PhotosController(
        IPhotoService photoService) : BaseController
    {

        /// <summary>
        /// Uploads one or more photos provided in the request.
        /// </summary>
        /// <param name="photos">A list of files representing the photos to upload. Each file must be provided as part of the
        /// multipart/form-data request body. Cannot be null.</param>
        /// <returns>An IActionResult indicating the result of the upload operation.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadPhotos([FromForm] IList<IFormFile> photos)
        {
            if (photos is null || !photos.Any())
            {
                return FailResponse(
                    message: "No photos uploaded.",
                    errors:
                    [
                        new ApiError
                        {
                            Code = ErrorCodes.BadRequest,
                            Field = "Photos",
                            Message = "You must upload at least one photo."
                        }
                    ]);
            }

            var photosDtos = photos.Select(
                    f => new PhotoUploadDto
                    (
                        FileName: f.FileName ?? string.Empty,
                        ContentType: f.ContentType ?? string.Empty,
                        Length: f.Length,
                        Content: f.OpenReadStream(),
                        Extension: Path.GetExtension(f.FileName ?? "")
                    )
                ).ToList();

            await photoService.UploadAsync(photosDtos);
            return Ok();
        }
    }
}
