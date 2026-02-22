using Microsoft.Extensions.Logging;

namespace MyBudgetIA.Application.Photo.Logs
{
    /// <summary>
    /// Provides logging utilities and message templates for photo service operations
    /// </summary>
    internal static partial class PhotoServiceLogs
    {
        #region UploadPhotoAsync

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Upload batch started. PhotosCount={Count}")]
        public static partial void LogStartedUploadingPhoto(this ILogger logger, int count);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Too many photos provided. Max {MaxPhotosCount} photos allowed per request, but {ProvidedPhotosCount} were provided.")]
        public static partial void LogTooManyPhotosProvided(this ILogger logger, int maxPhotosCount, int providedPhotosCount);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Creating blob for photo having name: {FileName}.")]
        public static partial void LogStartingBlobUpload(this ILogger logger, string fileName);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Successfully uploaded {FileName} as blob {BlobName}.")]
        public static partial void LogBlobUploadSuccess(this ILogger logger, string fileName, string blobName);

        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to upload {FileName} as blob {BlobName}. Error: {ErrorMessage}")]
        public static partial void LogBlobUploadFailed(this ILogger logger, string fileName, string blobName, string errorMessage);

        [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Validation failed for {FileName}. Messages={Messages}")]
        public static partial void LogBlobValidationFailed(this ILogger logger, string fileName, IReadOnlyCollection<string> messages);

        [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Successfully sent message to the queue for blob {BlobName}.")]
        public static partial void LogQueuePushSuccess(this ILogger logger, string blobName);

        [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Failed to send message to the queue for blob {BlobName}. Error: {ErrorMessage}. ErrorCode: {ErrorCode}")]
        public static partial void LogQueuePushFailed(this ILogger logger, string blobName, string errorMessage, string errorCode);

        #endregion

        #region DownloadPhotoAsync

        [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Started downloading photo with blob name '{BlobName}'.")]
        public static partial void LogStartBlobDownload(this ILogger logger, string blobName);

        [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Successfully downloaded photo from blob {BlobName}.")]
        public static partial void LogBlobDownloadSuccesss(this ILogger logger, string blobName);

        #endregion

        #region ListingUploadedPhotoAsync

        [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Started retrieving list of uploaded photos.")]
        public static partial void LogStartBlobsListing(this ILogger logger);

        [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Found {BlobsCount} photos in storage.")]
        public static partial void LogBlobsListingSuccess(this ILogger logger, int blobsCount);

        #endregion
    }
}
