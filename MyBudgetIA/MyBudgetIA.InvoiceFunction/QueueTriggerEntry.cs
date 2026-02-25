using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MyBudgetIA.Application.InvoicesProcess;
using MyBudgetIA.Application.InvoicesProcess.Dtos;
using MyBudgetIA.InvoiceFunction.ServiceLogs;
using Shared.Exceptions;
using Shared.Helpers;
using Shared.LoggingContext;
using Shared.Storage.DTOS;
using System.Text.Json;

namespace MyBudgetIA.InvoiceFunction;

public class QueueTriggerEntry(
    IInvoiceProcessService processorService,
    ILogger<QueueTriggerEntry> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(QueueTriggerEntry))]
    public async Task Run(
        [QueueTrigger("%QueueStorageQueueName%", Connection = "StorageConnectionString")] QueueMessage message,
        CancellationToken cancellationToken)
    {
        logger.LogStartedInvoiceProcess(message.MessageId);

        var payload = DeserializePayload(message);

        if (string.IsNullOrWhiteSpace(payload.TrackingId) || string.IsNullOrWhiteSpace(payload.BlobName))
            throw new FunctionalException(Messages.FunctionalExceptionMessage.Payload_Empty);

        var context = new InvoiceLoggingContext(
            payload.TrackingId,
            payload.BlobName,
            message.MessageId,
            message.DequeueCount);

        var request = new InvoiceProcessingRequest(payload.TrackingId, payload.BlobName);

        using (logger.BeginScope(context.ToScope()))
        {
            logger.LogBeginScopeContext(payload.BlobName, payload.TrackingId, message.DequeueCount);

            try
            {
                await processorService.ProcessAsync(request, context, cancellationToken);
                logger.LogInvoiceProcessSuccess();
            }
            catch (FunctionalException ex)
            {
                logger.LogInvoiceProcessFunctionalError(message.MessageId, ex.Message);
                // no retry
            }
            catch (TransientException)
            {
                logger.LogInvoiceProcessTransientError(message.MessageId);
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInvoiceProcessCanceled(message.MessageId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogInvoiceProcessUnexpectedError(message.MessageId, ex.Message);
                throw;
            }
        }
    }

    private static QueueMessageRequest DeserializePayload(QueueMessage message)
    {
        try
        {
            return JsonSerializer.Deserialize<QueueMessageRequest>(message.MessageText, JsonOptions)
                ?? throw new FunctionalException(
                    Messages.FunctionalExceptionMessage.Deserialization_Null(message.MessageId, message.DequeueCount));
        }
        catch (JsonException ex)
        {
            throw new FunctionalException(
                Messages.FunctionalExceptionMessage.Json_Exception(message.MessageId, message.DequeueCount),
                ex);
        }
        catch (FunctionalException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FunctionalException(Messages.FunctionalExceptionMessage.Invalid_Format, ex);
        }
    }

    [ExposedOnlyToUnitTests]
    internal static class Messages
    {
        public static class FunctionalExceptionMessage
        {
            public static string Deserialization_Null(string messageId, long dequeueCount) =>
                $"Message payload with id '{messageId}' is null after deserialization. DequeueCount:{dequeueCount}";

            public const string Invalid_Format = "Invalid queue message format.";
            public const string Payload_Empty = "Queue message payload missing required fields.";

            public static string Json_Exception(string messageId, long dequeueCount) =>
                $"JSON deserialization error for message with id '{messageId}' and dequeue count '{dequeueCount}'.";
        }
    }
}