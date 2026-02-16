using Microsoft.Extensions.Logging;
using MyBudgetIA.Application.Exceptions;
using MyBudgetIA.Application.Helpers;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Domain.Constraints;
using Shared.Helpers;
using Shared.Models;
using System.Reflection.Metadata;

namespace MyBudgetIA.Application.Photo
{
    /// <inheritdoc cref="IPhotoService"/>
    public class PhotoService(
        IValidationService validationService,
        IBlobStorageService blobStorageService,
        ILogger<PhotoService> logger) : IPhotoService
    {
        #region UploadPhotoAsync

        /// <inheritdoc/>
        public async Task<UploadPhotosResult> UploadPhotoAsync(
            IReadOnlyCollection<IFileUploadRequest> photos,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation(Messages.UploadPhotoAsync.Start_Uploading);

            if (photos.Count > PhotoConstraints.MaxPhotosPerRequest)
            {
                logger.LogError(
                    Messages.UploadPhotoAsync.TooManyPhotosProvided_2,
                    PhotoConstraints.MaxPhotosPerRequest,
                    photos.Count);

                throw new MaxPhotoCountExceptions(PhotoConstraints.MaxPhotosPerRequest, photos.Count);
            }

            List<BlobUploadResult> results = [];

            foreach (var photo in photos)
            {
                try
                {
                    // OPTION 1 : no try catch here, let validation exception bubble up and fail the entire request if any photo is invalid
                    // means no upload, and better validation error details returned to client
                    await validationService.ValidateAndThrowAllAsync(photo, cancellationToken);

                    LogStartingUpload(photo);

                    await using var stream = photo.OpenReadStream();

                    ValidateStreamOrThrow(photo, stream);

                    string trackingId = Guid.NewGuid().ToString("N");

                    string blobName = BlobNameBuilder.GenerateUniqueBlobName("photos", photo.FileName, trackingId);

                    BlobUploadRequest blobRequest = new(photo.FileName, blobName, stream, photo.ContentType, trackingId);

                    BlobUploadResult result = await blobStorageService.UploadFileAsync(blobRequest, cancellationToken);

                    if (result.IsSuccess)
                    {
                        logger.LogInformation(Messages.UploadPhotoAsync.UploadSuccess_2, photo.FileName, blobName);
                    }
                    else
                    {
                        logger.LogError(Messages.UploadPhotoAsync.UploadError_3, photo.FileName, blobName, result.ErrorMessage);
                    }

                    results.Add(result);
                }
                catch (ValidationException ex)
                {
                    // OPTION 2: Log validation errors and continue with next photos, returning validation failures in the result
                    // --> errors returned are not really explicits.. "validation error"
                    logger.LogWarning(ex, Messages.UploadPhotoAsync.ValidationFailed, photo.FileName);

                    results.Add(BlobUploadResult.CreateValidationFailure(
                        fileName: photo.FileName,
                        errorMessage: ex.Message));
                }
            }

            return new UploadPhotosResult(results);
        }

        private void LogStartingUpload(IFileUploadRequest photo)
        {
            logger.LogInformation(
                Messages.UploadPhotoAsync.Log_Creating_2,
                nameof(IFileUploadRequest),
                photo.FileName);
        }

        private static void ValidateStreamOrThrow(
            IFileUploadRequest photo,
            Stream? stream)
        {
            var errors = new ValidationErrors();
            var field = nameof(IFileUploadRequest.OpenReadStream);

            if (stream is null)
            {
                errors.Add(field, Messages.StreamValidation.StreamMustNotBeNull);
                errors.ThrowIfAny();
                return;
            }

            if (!stream.CanRead)
            {
                errors.Add(field, Messages.StreamValidation.StreamMustBeReadable);
            }

            if (stream.CanSeek && stream.Position != 0)
            {
                stream.Position = 0; // normalization
            }

            if (stream.CanSeek && stream.Length != photo.Length)
            {
                errors.Add(field, Messages.StreamValidation.StreamLengthMustMatchProvidedLength);
            }

            errors.ThrowIfAny();
        }

        #endregion

        #region DownloadPhotoAsync

        /// <inheritdoc/>
        public async Task<DownloadedPhotoDto> DownloadPhotoAsync(
            string blobName,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation(Messages.DownloadPhotoAsync.Start_Downloading, blobName);

            var blob = await blobStorageService.DownloadBlobAsync(blobName, cancellationToken);

            logger.LogInformation(Messages.DownloadPhotoAsync.DownloadSuccess, blobName);

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
            logger.LogInformation(Messages.ListingUploadedPhotoAsync.Start_Retrieving_List);

            var blobsData = await blobStorageService.GetBlobsInfoAsync(cancellationToken);

            logger.LogInformation(Messages.ListingUploadedPhotoAsync.ListingSuccess);

            return blobsData;
        }

        #endregion

        [ExposedOnlyToUnitTests]
        internal static class Messages
        {
            public static class UploadPhotoAsync
            {
                public const string Start_Uploading = "Started uploading pictures.";
                public const string Log_Creating_2 = "Creating {TypeName} with BlobName: {BlobName}.";
                public const string TooManyPhotosProvided_2 = "Too many photos provided. Maximum {MaxPhotosAllowed} photos allowed per request, but {PhotosProvided} were provided.";
                public const string UploadSuccess_2 = "Successfully uploaded {FileName} as blob {BlobName}.";
                public const string UploadError_3 = "Failed to upload {FileName} as blob {BlobName}. Error: {ErrorMessage}";
                public const string ValidationFailed = "Validation failed for {FileName}.";
            }

            public static class StreamValidation
            {
                public const string StreamMustNotBeNull = "Stream can not be null.";
                public const string StreamMustBeReadable = "Stream must be readable.";
                public const string StreamLengthMustMatchProvidedLength = "Stream length does not match file length.";
            }

            public static class DownloadPhotoAsync
            {
                public const string Start_Downloading = "Started downloading photo with blob name '{BlobName}'.";
                public const string DownloadSuccess = "Successfully downloaded photo from blob '{BlobName}'.";
            }

            public static class ListingUploadedPhotoAsync
            {
                public const string Start_Retrieving_List = "Started retrieving list of uploaded photos.";
                public const string ListingSuccess = "Successfully retrieved list of uploaded photos.";
            }
        }
    }
}