using Microsoft.AspNetCore.Mvc;
using MyBudgetIA.Api.Uploads;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Application.Photo.Dtos;
using Shared.Models;

namespace MyBudgetIA.Api.Controllers
{
    public class PhotosController(IPhotoService photoService) : BaseController
    {
        /// <summary>
        /// Uploads one or more photos and returns the result of each upload operation.
        /// </summary>
        /// <remarks>The method requires at least one photo to be provided. If no photos are uploaded, a
        /// 400 Bad Request response is returned with an error message. Each uploaded photo is processed individually,
        /// and the result for each is included in the response.</remarks>
        /// <param name="photos">A collection of photo files to upload. Must contain at least one file; otherwise, the request will fail.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the upload operation.</param>
        /// <returns>An IActionResult containing an ApiResponse with a list of BlobUploadResult objects if the upload succeeds,
        /// or an ApiResponse with error details if the request is invalid.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UploadPhotosResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadPhotos([FromForm] IList<IFormFile> photos, CancellationToken ct)
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

            var uploadedFiles = photos.Select(f => new FormFileAdapter(f)).ToList();

            var result = await photoService.UploadPhotoAsync(uploadedFiles, ct);

            var message = result.IsSuccess
                ? "Upload completed."
                : result.IsPartialSuccess
                    ? "Upload partially completed."
                    : "Upload failed.";

            return Ok(new ApiResponse<UploadPhotosResult>
            {
                Success = result.IsSuccess,
                Message = message,
                Data = result,
                Errors = []
            });
        }
    }
}
