using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using Shared.Models;

namespace MyBudgetIA.Infrastructure.Storage
{
    /// <inheritdoc cref="IBlobStorageService"/>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient container;
        private readonly ILogger<BlobStorageService> logger;

        /// <summary>
        /// Initializes a new instance of the BlobStorageService class using the specified BlobServiceClient and storage
        /// settings.
        /// </summary>
        /// <param name="blobService">The BlobServiceClient instance used to interact with the Azure Blob Storage service. Cannot be null.</param>
        /// <param name="options">The options containing BlobStorageSettings, including the container name to use. Cannot be null.</param>
        /// <param name="logger">The logger instance for logging operations within the service. Cannot be null.</param>
        public BlobStorageService(
            IAzureBlobServiceClient blobService,
            IOptions<BlobStorageSettings> options,
            ILogger<BlobStorageService> logger)
        {
            var settings = options.Value;
            container = blobService.GetBlobContainerClient(settings.ContainerName);
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<BlobUploadResult> UploadFileAsync(
            BlobUploadRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryValidateUploadRequest(request, out var failure))
            {
                logger.LogFailedBlobRequestValidation();
                return failure;
            }

            logger.LogStartedUploadingBlob(request.BlobName, container.Name);

            var blob = container.GetBlobClient(request.BlobName);

            BlobUploadOptions uploadOptions = new()
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = request.ContentType },
                Metadata = BlobMetadata.Create(request.ContentType, request.TrackingId, request.CreatedAt),
                Conditions = new BlobRequestConditions
                {
                    // avoid overwriting existing blobs
                    IfNoneMatch = ETag.All
                }
            };

            try
            {
                var response = await blob.UploadAsync(request.Stream, uploadOptions, cancellationToken);

                logger.LogSuccessUploadBlob(request.BlobName, request.TrackingId, container.Name);

                return BlobUploadResult.CreateSuccess(
                    fileName: request.FileName,
                    trackingId: request.TrackingId,
                    blobName: request.BlobName,
                    etag: response.Value.ETag.ToString().Trim('"'));
            }
            //catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            //{   // consider all implications of a cancellation..
            //    throw;
            //}
            catch (RequestFailedException ex)
            {
                var errorCode = AzureBlobErrorCodes.MapAzureBlobErrorCode(ex);

                logger.LogAzureBlobUploadError(request.BlobName, ex.Status, errorCode);

                return BlobUploadResult.CreateFailure(
                    fileName: request.FileName,
                    trackingId: request.TrackingId,
                    blobName: request.BlobName,
                    errorMessage:StorageErrorMessages.AzureBlobUploadFailed,
                    errorCode: errorCode);
            }
            catch (Exception)
            {
                logger.LogAzureBlobUploadUnexpectedError(request.BlobName);

                return BlobUploadResult.CreateFailure(
                    fileName: request.FileName,
                    trackingId: request.TrackingId,
                    blobName: request.BlobName,
                    errorMessage: StorageErrorMessages.UnexpectedUploadFailure,
                    errorCode: ErrorCodes.BlobStorageError);
            }
        }

        private static bool TryValidateUploadRequest(
            BlobUploadRequest request,
            out BlobUploadResult failure)
        {
            // Keep infra validation minimal and generic: Application layer already provides detailed validation.
            if (string.IsNullOrWhiteSpace(request.FileName)
                || string.IsNullOrWhiteSpace(request.BlobName)
                || string.IsNullOrWhiteSpace(request.TrackingId)
                || string.IsNullOrWhiteSpace(request.ContentType)
                || request.CreatedAt == default
                || request.CreatedAt.Kind != DateTimeKind.Utc
                || request.Stream is null
                || !request.Stream.CanRead)
            {
                failure = BlobUploadResult.CreateFailure(
                    fileName: request.FileName ?? string.Empty,
                    trackingId: request.TrackingId ?? string.Empty,
                    blobName: request.BlobName ?? string.Empty,
                    errorMessage: StorageErrorMessages.ValidationFailed,
                    errorCode: ErrorCodes.BlobValidationFailed);

                return false;
            }

            if (request.Stream.CanSeek && request.Stream.Position != 0)
            {
                request.Stream.Position = 0;
            }

            failure = default!;
            return true;
        }
    }
}