using Microsoft.Extensions.Logging;
using MyBudgetIA.Application.Exceptions;
using MyBudgetIA.Application.Helpers;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Application.Photo.Dtos.Blob;
using MyBudgetIA.Application.Photo.Logs;
using MyBudgetIA.Application.TechnicalServices;
using MyBudgetIA.Domain.Constraints;
using Shared.Helpers;
using Shared.Storage.DTOS;

namespace MyBudgetIA.Application.Photo
{
    /// <inheritdoc cref="IPhotoService"/>
    public class PhotoService(
        IValidationService validationService,
        IStreamValidationService streamValidation,
        IBlobStorageService blobStorageService,
        IQueueStorageService queueStorageService,
        ILogger<PhotoService> logger) : IPhotoService
    {
        #region UploadPhotoAsync

        /// <inheritdoc/>
        public async Task<(IEnumerable<UploadPhotosResult> result, string message)> UploadPhotoAsync(
            IReadOnlyCollection<IFileUploadRequest> photos,
            CancellationToken cancellationToken = default)
        {
            logger.LogStartedUploadingPhoto(photos.Count);

            if (photos.Count > PhotoConstraints.MaxPhotosPerRequest)
            {
                logger.LogTooManyPhotosProvided(PhotoConstraints.MaxPhotosPerRequest, photos.Count);
                throw new MaxPhotoCountExceptions(PhotoConstraints.MaxPhotosPerRequest, photos.Count);
            }

            List<UploadPhotosResult> totalResult = [];

            foreach (var photo in photos)
            {
                try
                {
                    // OPTION 1 : no try catch here, let validation exception bubble up and fail the entire request if any photo is invalid
                    await validationService.ValidateAndThrowAllAsync(photo, cancellationToken);

                    logger.LogStartingBlobUpload(photo.FileName);

                    await using var stream = photo.OpenReadStream();

                    streamValidation.ValidateStreamOrThrow(photo.Length, stream);

                    string trackingId = Guid.NewGuid().ToString("N");

                    string blobName = BlobNameBuilder.GenerateUniqueBlobName(
                        Messages.Constants.BlobContainerName,
                        photo.FileName,
                        trackingId);

                    var photoUploadResult = new UploadPhotosResult(blobName, photo.FileName, trackingId, photo.ContentType);

                    BlobUploadRequest blobRequest = new(photo.FileName, blobName, stream, photo.ContentType, trackingId);

                    BlobUploadResult blobUploadResult = await blobStorageService.UploadFileAsync(blobRequest, cancellationToken);

                    //List<PhotoUploadResult> blobName, FileName, TrackingId, contentType, StartedAt
                    //, blob[iSuccess, errorMessage, ErrorCode, etag, LastModifiedUtc],
                    // queue[iSuccess, errorMessage, ErrorCode, LastModifiedUtc, MessageId] 

                    if (blobUploadResult.IsSuccess)
                    {
                        logger.LogBlobUploadSuccess(photo.FileName, blobName);

                        var queueResult = await queueStorageService.EnqueueAsync(new QueueMessageRequest(blobName, trackingId), cancellationToken);

                        if (queueResult.IsSuccess)
                        {
                            logger.LogQueuePushSuccess(blobName);
                        }
                        else
                        {
                            logger.LogQueuePushFailed(
                                blobName,
                                queueResult.ErrorMessage!,
                                queueResult.ErrorCode! );
                        }

                        photoUploadResult.QueueResult = queueResult;
                    }
                    else
                    {
                        logger.LogBlobUploadFailed(photo.FileName, blobName, blobUploadResult.ErrorMessage!); // <-- we ensure errorMessage is not null in case of failure
                    }

                    photoUploadResult.BlobResult = blobUploadResult;

                    totalResult.Add(photoUploadResult);
                }
                catch (ValidationException ex)
                {
                    // OPTION 2: Log validation errors and continue with next photos, returning validation failures in the blobUploadResult
                    logger.LogBlobValidationFailed(photo.FileName, [.. ex.Errors.SelectMany(e => e.Value)]);

                    totalResult.Add(UploadPhotosResult.CreateValidationFailure(
                        fileName: photo.FileName,
                        errors: ex.Errors));
                }
            }

            string message = BuildSuccessMessage(totalResult);

            return (totalResult, message);
        }

        private static string BuildSuccessMessage(List<UploadPhotosResult> totalResult)
        {
            return totalResult.All(r => r.IsSuccess) ?
                Messages.SuccesMessage.AllPhotosUploadedSuccessfully :
                totalResult.Any(r => r.IsSuccess) ?
                    Messages.SuccesMessage.SomePhotosUploadedSuccessfully :
                    Messages.SuccesMessage.FailedToUploadAllPhotos;
        }

        #endregion

        #region DownloadPhotoAsync

        /// <inheritdoc/>
        public async Task<DownloadedPhotoDto> DownloadPhotoAsync(
            string blobName,
            CancellationToken cancellationToken = default)
        {
            logger.LogStartBlobDownload(blobName);

            var blob = await blobStorageService.DownloadBlobAsync(blobName, cancellationToken);

            logger.LogBlobDownloadSuccesss(blobName);

            return new DownloadedPhotoDto
            {
                Content = blob.Content,
                FileName = blob.FileName ?? blobName,
                ContentType = blob.ContentType
            };
        }

        #endregion

        #region GetUploadedPhotosInfosAsync

        /// <inheritdoc/>
        public async Task<IEnumerable<BlobData>> GetUploadedPhotosInfosAsync(CancellationToken cancellationToken = default)
        {
            logger.LogStartBlobsListing();

            var blobsData = await blobStorageService.GetBlobsInfoAsync(cancellationToken);

            logger.LogBlobsListingSuccess(blobsData.Count());

            return blobsData;
        }

        #endregion

        [ExposedOnlyToUnitTests]
        internal static class Messages
        {
            public static class SuccesMessage
            {
                public const string AllPhotosUploadedSuccessfully = "All photos uploaded successfully with message.";
                public const string SomePhotosUploadedSuccessfully = "Some photos uploaded successfully, some failed.";
                public const string FailedToUploadAllPhotos = "Failed to upload all photos.";
            }

            public static class Constants
            {
                public const string BlobContainerName = "photos";
            }
        }
    }
}