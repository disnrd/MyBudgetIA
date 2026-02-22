using Azure.Storage.Queues;
using Microsoft.Extensions.Logging.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using MyBudgetIA.Infrastructure.Storage.Blob;
using MyBudgetIA.Infrastructure.Storage.Queue;
using Shared.Storage.DTOS;
using System.Text.Json;

namespace MyBudgetIA.Infrastructure.Tests.Storage
{
    /// <summary>
    /// Integration tests for the <see cref="QueueStorageService"/> class.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class QueueStorageServiceIntegrationTests
    {
        private const string ConnectionString = "UseDevelopmentStorage=true";
        private const string QueueName = "photos-queue-integration-tests";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private QueueClient queueClient;
        private QueueStorageService queueStorageService;
        private IAzureStorageErrorMapper azureStorageErrorMapper;

        #region SetUp and TearDown

        [SetUp]
        public async Task SetUp()
        {
            queueClient = new QueueClient(ConnectionString, QueueName);
            azureStorageErrorMapper = new AzureStorageErrorMapper(
                 new AzureStorageErrorMapperFactory(
                     new AzureBlobErrorMapper(),
                     new AzureQueueErrorMapper()
                 )
            );

            await queueClient.CreateIfNotExistsAsync();
            await queueClient.ClearMessagesAsync();

            var adapter = new AzureQueueServiceClientAdapter(queueClient);

            queueStorageService = new QueueStorageService(
                queueServiceClient: adapter,
                azureStorageErrorMapper,
                logger: NullLogger<QueueStorageService>.Instance);
        }

        [TearDown]
        public async Task TearDown()
        {
            // Keep or not? 
            await queueClient.DeleteIfExistsAsync();
            await Task.CompletedTask;
        }

        #endregion

        [Test]
        public async Task QueueStorageService_EnqueueAsync_ShouldEnqueue_AndBeReceivable()
        {
            // Arrange
            var request = new QueueMessageRequest(
                BlobName: $"photos/{Guid.NewGuid():N}.png",
                TrackingId: Guid.NewGuid().ToString("N"));

            // Act
            var result = await queueStorageService.EnqueueAsync(request, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.ErrorCode, Is.Null);
                Assert.That(result.ErrorMessage, Is.Null);
                Assert.That(result.MessageId, Is.Not.Null.And.Not.Empty);

                Assert.That(result.LastModifiedUtc, Is.GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5)));
                Assert.That(result.LastModifiedUtc, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(1)));
            }

            // Assert (storage state) - peek test
            var peeked = await queueClient.PeekMessageAsync(CancellationToken.None);
            Assert.That(peeked.Value, Is.Not.Null);

            // Assert (storage state) - received message test
            var received = await queueClient.ReceiveMessageAsync(cancellationToken: CancellationToken.None);

            Assert.That(received.Value, Is.Not.Null, "No message received from the queue.");

            var body = received.Value.Body.ToString();
            var payload = JsonSerializer.Deserialize<QueueMessageRequest>(body, JsonOptions);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(received.Value.MessageId, Is.EqualTo(result.MessageId));
                Assert.That(payload, Is.Not.Null);
                Assert.That(payload!.BlobName, Is.EqualTo(request.BlobName));
                Assert.That(payload.TrackingId, Is.EqualTo(request.TrackingId));
            }

            // Cleanup message
            await queueClient.DeleteMessageAsync(received.Value.MessageId, received.Value.PopReceipt, CancellationToken.None);
        }
    }
}
