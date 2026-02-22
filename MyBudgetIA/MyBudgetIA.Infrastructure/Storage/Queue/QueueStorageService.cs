using Azure;
using Microsoft.Extensions.Logging;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo.Dtos.Queue;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using MyBudgetIA.Infrastructure.Storage.Blob;
using Shared.Models;
using Shared.Storage.DTOS;
using System.Text.Json;

namespace MyBudgetIA.Infrastructure.Storage.Queue
{
    ///  <inheritdoc cref="IQueueStorageService"/>
    public sealed class QueueStorageService(
        IAzureQueueServiceClient queueServiceClient,
        IAzureStorageErrorMapper azureStorageErrorMapper,
        ILogger<QueueStorageService> logger) : IQueueStorageService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        /// <inheritdoc/>
        public async Task<QueuePushResult> EnqueueAsync(
            QueueMessageRequest request,
            CancellationToken ct = default)
        {
            if (!TryValidateUploadRequest(request, out var failure))
            {
                logger.LogFailedQueueRequestValidation();

                return failure;
            }

            logger.LogStartedSendingMessage(request.BlobName, request.TrackingId);

            try
            {
                var json = JsonSerializer.Serialize(request, JsonOptions);

                var (MessageId, InsertionTime) = await queueServiceClient.SendMessageAsync(json, ct);

                logger.LogSuccessSendingMessage(request.BlobName, request.TrackingId);

                return QueuePushResult.CreateSuccess(
                    MessageId,
                    InsertionTime.ToUniversalTime());

            }
            //catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            //{   // consider all implications of a cancellation..
            //    throw;
            //}
            catch (RequestFailedException ex)
            {
                var errorCode = azureStorageErrorMapper.Map(ex, StorageOperationType.QueueMessageSending);
              
                logger.LogAzureQueueError(request.BlobName, ex.Status, errorCode);

                return QueuePushResult.CreateFailure(
                    errorMessage: StorageErrorMessages.QueuePushFailed,
                    errorCode: errorCode);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                logger.LogQueueMessageSerializationFailed(request.BlobName);

                return QueuePushResult.CreateFailure(
                    errorMessage: StorageErrorMessages.FailedToSerializeQueueMessage,
                    errorCode: ErrorCodes.QueueMessageSerializationError);
            }
            catch (Exception)
            {
                logger.LogAzureQueueUnexpectedError(request.BlobName);

                return QueuePushResult.CreateFailure(
                    errorMessage: StorageErrorMessages.QueuePushUnexpectedFailed,
                    errorCode: ErrorCodes.QueueStorageError);
            }
        }

        private static bool TryValidateUploadRequest(
            QueueMessageRequest request,
            out QueuePushResult failure)
        {
            if (string.IsNullOrWhiteSpace(request.BlobName)
                || string.IsNullOrWhiteSpace(request.TrackingId))
            {
                failure = QueuePushResult.CreateFailure(
                    errorMessage: StorageErrorMessages.QueueRequestValidationFailed,
                    errorCode: ErrorCodes.QueueValidationFailed);

                return false;
            }

            failure = default!;
            return true;
        }
    }
}
