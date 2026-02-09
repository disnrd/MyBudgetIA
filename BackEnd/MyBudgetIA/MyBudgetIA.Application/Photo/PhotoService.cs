using Microsoft.Extensions.Logging;
using MyBudgetIA.Application.Exceptions;
using MyBudgetIA.Application.Helpers;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Domain.Constraints;
using Shared.Helpers;

namespace MyBudgetIA.Application.Photo
{
    /// <inheritdoc cref="IPhotoService"/>
    public class PhotoService(
        IValidationService validationService,
        IBlobStorageService blobStorageService,
        ILogger<PhotoService> logger) : IPhotoService
    {
        #region UploadAsync

        /// <inheritdoc/>
        public async Task<UploadPhotosResult> UploadPhotoAsync(
            IReadOnlyCollection<IFileUploadRequest> photos,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation(Messages.UploadAsync.Start_Uploading);

            if (photos.Count > PhotoConstraints.MaxPhotosPerRequest)
            {
                logger.LogError(
                    Messages.UploadAsync.TooManyPhotosProvided_2,
                    PhotoConstraints.MaxPhotosPerRequest,
                    photos.Count);

                throw new MaxPhotoCountExceptions(PhotoConstraints.MaxPhotosPerRequest, photos.Count);
            }

            List<BlobUploadResult> results = [];

            foreach (var photo in photos)
            {
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
                    logger.LogInformation(
                        Messages.UploadAsync.UploadSuccess_2,
                        photo.FileName,
                        blobName);
                }
                else
                {
                    logger.LogError(
                        Messages.UploadAsync.UploadError_3,
                        photo.FileName,
                        blobName,
                        result.ErrorMessage);
                }

                results.Add(result);
            }

            return new UploadPhotosResult(results);
        }

        private void LogStartingUpload(IFileUploadRequest photo)
        {
            logger.LogInformation(
                Messages.UploadAsync.Log_Creating_2,
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

        [ExposedOnlyToUnitTests]
        internal static class Messages
        {
            public static class UploadAsync
            {
                public const string Start_Uploading = "Started uploading pictures.";
                public const string Log_Creating_2 = "Creating {TypeName} with BlobName: {BlobName}.";
                public const string TooManyPhotosProvided_2 = "Too many photos provided. Maximum {MaxPhotosAllowed} photos allowed per request, but {PhotosProvided} were provided.";
                public const string UploadSuccess_2 = "Successfully uploaded {FileName} as blob {BlobName}.";
                public const string UploadError_3 = "Failed to upload {FileName} as blob {BlobName}. Error: {ErrorMessage}";
            }

            public static class StreamValidation
            {
                public const string StreamMustNotBeNull = "Stream can not be null.";
                public const string StreamMustBeReadable = "Stream must be readable.";
                public const string StreamLengthMustMatchProvidedLength = "Stream length does not match file length.";
            }
        }
    }
}
