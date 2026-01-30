using Microsoft.Extensions.Logging;
using MyBudgetIA.Application.Exceptions;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Domain.Constraints;
using Shared.Helpers;
using System.Globalization;

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
        public async Task UploadAsync(IReadOnlyCollection<PhotoUploadDto> photos)
        {
            logger.LogInformation(Messages.UploadAsync.Start_Uploading);

            if(photos.Count > PhotoConstraints.MaxPhotosPerRequest)
            {
                logger.LogError(
                    Messages.UploadAsync.TooManyPhotosProvided_2,
                    PhotoConstraints.MaxPhotosPerRequest,
                    photos.Count);
                throw new MaxPhotoCountExceptions(PhotoConstraints.MaxPhotosPerRequest, photos.Count);
            }

            foreach (PhotoUploadDto photo in photos)
            {
                await validationService.ValidateAndThrowAsync(photo);

                LogStartingUpload(photo);

                // forcer position a 0 au cas ou le stream a ete lu avant
                // blobStorageService.UploadFile();

                throw new NotImplementedException();
            }
        }

        private void LogStartingUpload(PhotoUploadDto photo)
        {
            logger.LogInformation(
                Messages.UploadAsync.Log_Creating_2,
                nameof(PhotoUploadDto),
                photo.FileName);
        }

        #endregion

        [ExposedOnlyToUnitTests]
        internal static class Messages
        {
            public static class UploadAsync
            {
                public const string Start_Uploading = "Started uploading pictures.";

                public const string Log_Creating_2 = "Creating {TypeName} with FileName: {FileName}.";

                public const string TooManyPhotosProvided_2 = "Too many photos provided. Maximum {MaxPhotosAllowed} photos allowed per request, but {PhotosProvided} were provided.";
            }
        }
    }
}
