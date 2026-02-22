using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo.Dtos.Blob;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Exceptions;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using Shared.Models;
using Shared.Storage.DTOS;

namespace MyBudgetIA.Infrastructure.Storage.Blob
{
    /// <inheritdoc cref="IBlobStorageService"/>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient container;
        private readonly IAzureStorageErrorMapper azureStorageErrorMapper;
        private readonly ILogger<BlobStorageService> logger;

        /// <summary>
        /// Initializes a new instance of the BlobStorageService class using the specified BlobServiceClient and storage
        /// settings.
        /// </summary>
        /// <param name="blobService">The BlobServiceClient instance used to interact with the Azure Blob Storage service. Cannot be null.</param>
        /// <param name="options">The options containing BlobStorageSettings, including the container name to use. Cannot be null.</param>
        /// <param name="azureStorageErrorMapper">The error mapper instance for translating Azure storage exceptions into user-friendly messages. Cannot be null.</param>
        /// <param name="logger">The logger instance for logging operations within the service. Cannot be null.</param>
        public BlobStorageService(
            IAzureBlobServiceClient blobService,
            IOptions<BlobStorageSettings> options,
            IAzureStorageErrorMapper azureStorageErrorMapper,
            ILogger<BlobStorageService> logger)
        {
            var settings = options.Value;
            container = blobService.GetBlobContainerClient(settings.ContainerName);
            this.azureStorageErrorMapper = azureStorageErrorMapper;
            this.logger = logger;
        }

        #region UploadFileAsync

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
                Metadata = BlobMetadata.Create(request.FileName, request.TrackingId),
                Conditions = new BlobRequestConditions
                {
                    // avoid overwriting existing blobs
                    IfNoneMatch = ETag.All
                }
            };

            try
            {
                var response = await blob.UploadAsync(request.Stream, uploadOptions, cancellationToken);

                logger.LogSuccessBlobUpload(request.BlobName, request.TrackingId, container.Name);

                return BlobUploadResult.CreateSuccess(
                    etag: response.Value.ETag.ToString().Trim('"'),
                    response.Value.LastModified.ToUniversalTime());
            }
            //catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            //{   // consider all implications of a cancellation..
            //    throw;
            //}
            catch (RequestFailedException ex)
            {
                var errorCode = azureStorageErrorMapper.Map(ex, StorageOperationType.BlobUpload);

                logger.LogAzureBlobUploadError(request.BlobName, ex.Status, errorCode);

                return BlobUploadResult.CreateFailure(
                    errorMessage: StorageErrorMessages.BlobUploadFailed,
                    errorCode: errorCode);
            }
            catch (Exception)
            {
                logger.LogAzureBlobUploadUnexpectedError(request.BlobName);

                return BlobUploadResult.CreateFailure(
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
                || request.Stream is null
                || !request.Stream.CanRead)
            {
                failure = BlobUploadResult.CreateFailure(
                    errorMessage: StorageErrorMessages.BlobRequestValidationFailed,
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

        #endregion

        #region DownloadBlobAsync

        /// <inheritdoc/>
        public async Task<BlobDownloadData> DownloadBlobAsync(string blobName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new BlobStorageException(
                    StorageErrorMessages.BlobNameValidationFailed,
                    nameof(blobName),
                    ErrorCodes.BlobStorageValidationError,
                    400);
            }

            logger.LogStartedDownloadingBlob(blobName);

            try
            {

                var blobClient = container.GetBlobClient(blobName);

                var blobResult = await blobClient.DownloadStreamingAsync(null, ct);

                logger.LogSuccessBlobDownload(blobName, container.Name);

                return new BlobDownloadData
                {
                    Content = blobResult.Value.Content,
                    ContentType = blobResult.Value.Details.ContentType ?? "application/octet-stream",
                    FileName = GetMetadataValue(blobResult.Value.Details.Metadata, nameof(BlobDownloadData.FileName)),
                    // trackingId est il un concept métier d'audit/tracing..domain?..
                    TrackingId = GetMetadataValue(blobResult.Value.Details.Metadata, nameof(BlobDownloadData.TrackingId)),
                    Metadata = blobResult.Value.Details.Metadata
                };

            }
            catch (RequestFailedException ex)
            {
                var errorCode = azureStorageErrorMapper.Map(ex, StorageOperationType.BlobDownload);
                logger.LogAzureBlobDownloadError(blobName, ex.Status, errorCode);

                throw new BlobStorageException(StorageErrorMessages.BlobDownloadFailed, blobName, errorCode, ex.Status);
            }
        }

        private static string? GetMetadataValue(
            IDictionary<string, string> metadata,
            string key)
        {
            return metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
        }

        #endregion

        #region GetBlobsInfoAsync

        /// <inheritdoc/>
        public async Task<IEnumerable<BlobData>> GetBlobsInfoAsync(CancellationToken ct = default)
        {
            logger.LogStartedGettingBlobListing(container.Name);

            try
            {
                var results = new List<BlobData>();

                // ATTENTION pour la pagination : azure blob ne définit pas d'ordre de listing
                await foreach (BlobItem blob in container.GetBlobsAsync(
                    traits: BlobTraits.Metadata,
                    states: BlobStates.None,
                    prefix: default,
                    ct))
                {
                    results.Add(new BlobData(
                        BlobName: blob.Name,
                        FileName: GetMetadataValue(blob.Metadata, nameof(BlobData.FileName)) ?? blob.Name,
                        LastModified: blob.Properties.LastModified));
                }

                logger.LogSuccessBlobListing(container.Name);

                return results;
            }
            catch (RequestFailedException ex)
            {
                var errorCode = azureStorageErrorMapper.Map(ex, StorageOperationType.BlobDownload);
                logger.LogAzureBlobListingError(container.Name, ex.Status, errorCode);

                throw new BlobStorageException(StorageErrorMessages.BlobsListFailed, container.Name, errorCode, ex.Status);
            }
        }

        #endregion
    }
}