using Microsoft.AspNetCore.Mvc;
using MyBudgetIA.Api.Uploads;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Application.Photo.Dtos;
using Shared.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                return BadRequestResponse("No photos uploaded.", [ApiErrors.RequiredWithMessage("Photos", "You must upload at least one photo.")]);
            }

            var uploadedFiles = photos.Select(f => new FormFileAdapter(f)).ToList();

            var result = await photoService.UploadPhotoAsync(uploadedFiles, ct);

            var message = result.IsSuccess
                ? "Upload completed."
                : result.IsPartialSuccess
                    ? "Upload partially completed."
                    : "Upload failed.";

            return OkResponse(result, message);
        }

        /// <summary>
        /// Retrieves a photo blob from storage and returns it as a file stream if found.
        /// </summary>
        /// <param name="blobName">The name of the blob to retrieve. This parameter cannot be null or whitespace.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An IActionResult containing the photo blob as a file stream if found; otherwise, a 404 Not Found or 400 Bad
        /// Request response.</returns>
        [HttpGet("blob/{blobName}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPhotoBlob(string blobName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(blobName))
                return BadRequestResponse("Blob name is required.", [ApiErrors.Required("BlobName")]);

            var result = await photoService.DownloadPhotoAsync(blobName, ct);

            if (result is null)
                return NotFoundResponse("Blob not found.", [ApiErrors.NotFound("BlobName", $"No blob found with the name '{blobName}'.")]);

            return File(result.Content, result.ContentType, result.FileName);
        }

        /// <summary>
        /// Retrieves information about all uploaded photo blobs.
        /// </summary>
        /// <param name="ct">The cancellation token used to propagate notification that the operation should be canceled.</param>
        /// <returns>An IActionResult containing an ApiResponse with a collection of BlobData representing the uploaded photos.</returns>
        [HttpGet("blobs")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BlobData>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUploadedPhotosInfos(CancellationToken ct)
        {
            // todo: Add pagination
            var result = await photoService.GetUploadedPhotosInfosAsync(ct);

            return OkResponse(result);
        }
    }
}
