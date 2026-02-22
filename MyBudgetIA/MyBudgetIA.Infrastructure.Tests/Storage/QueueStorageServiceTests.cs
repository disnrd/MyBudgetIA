using Azure;
using Microsoft.Extensions.Logging;
using Moq;
using MyBudgetIA.Infrastructure.Storage;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using MyBudgetIA.Infrastructure.Storage.Blob;
using MyBudgetIA.Infrastructure.Storage.Queue;
using Shared.Models;
using Shared.Storage.DTOS;
using Shared.TestsLogging;
using System.Text.Json;

namespace MyBudgetIA.Infrastructure.Tests.Storage
{
    /// <summary>
    /// Unit tests for the <see cref="QueueStorageService"/> class."/>
    /// </summary>
    [TestFixture]
    public class QueueStorageServiceTests
    {
        private Mock<IAzureQueueServiceClient> mockQueueClient;
        private TestLogger<QueueStorageService> logger;
        private Mock<IAzureStorageErrorMapper> mockAzureStorageErrorMapper;

        private QueueStorageService service;

        #region SetUp

        [SetUp]
        public void Setup()
        {
            mockQueueClient = new Mock<IAzureQueueServiceClient>();
            logger = new TestLogger<QueueStorageService>();
            mockAzureStorageErrorMapper = new Mock<IAzureStorageErrorMapper>();

            service = new QueueStorageService(
                mockQueueClient.Object,
                mockAzureStorageErrorMapper.Object,
                logger);
        }

        #endregion

        #region EnqueueAsync

        private static QueueMessageRequest CreateValidRequest()
        {
            var blobName = "test.txt";
            var trackingId = Guid.NewGuid().ToString("N");
            return new QueueMessageRequest(blobName, trackingId);
        }

        [Test]
        public async Task QueueStorageService_EnqueueAsync_InvalidRequest_Should_Return_Failure()
        {
            // Arrange
            var request = new QueueMessageRequest(string.Empty, string.Empty);

            // Act
            var result = await service.EnqueueAsync(request, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(ErrorCodes.QueueValidationFailed));
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.QueueRequestValidationFailed));
                var log = logger.Entries.Single(e => e.EventId.Id == 6);
                Assert.That(log.Level, Is.EqualTo(LogLevel.Warning));
            }
        }

        [Test]
        public async Task QueueStorageService_EnqueueAsync_ThrowRequestFailedException_ShouldCreateFailure()
        {
            // Arrange
            var request = CreateValidRequest();
            var status = 409;
            var expectedErrorCode = ErrorCodes.QueueAlreadyExists;

            mockQueueClient
                .Setup(c => c.SendMessageAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(
                    status: status, // <--
                    errorCode: expectedErrorCode, // <-- no need to test all cases, we will test mapper in his own unit tests
                    message: "failed",
                    innerException: null))
                .Verifiable();

            mockAzureStorageErrorMapper
                .Setup(m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.QueueMessageSending))
                .Returns(expectedErrorCode)
                .Verifiable();

            // Act
            var result = await service.EnqueueAsync(request, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(ErrorCodes.QueueAlreadyExists));
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.QueuePushFailed));

                var log = logger.Entries.Single(e => e.EventId.Id == 4);
                Assert.That(log.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<QueueStorageService>.GetStateValue(log, "BlobName"), Is.EqualTo(request.BlobName));
                Assert.That(TestLogger<QueueStorageService>.GetStateValue(log, "Status"), Is.EqualTo(status));
                Assert.That(TestLogger<QueueStorageService>.GetStateValue(log, "ErrorCode"), Is.EqualTo(expectedErrorCode));
            }

            mockQueueClient.Verify(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            mockAzureStorageErrorMapper.Verify(
                m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.QueueMessageSending),
                Times.Once);
        }

        [Test]
        public async Task QueueStorageService_EnqueueAsync_ThrowJsonException_ShouldCreateFailure()
        {
            // Arrange
            var request = CreateValidRequest();

            mockQueueClient
                .Setup(c => c.SendMessageAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException())
                .Verifiable();

            // Act
            var result = await service.EnqueueAsync(request, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(ErrorCodes.QueueMessageSerializationError));
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.FailedToSerializeQueueMessage));

                var log = logger.Entries.Single(e => e.EventId.Id == 7);
                Assert.That(log.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<QueueStorageService>.GetStateValue(log, "BlobName"), Is.EqualTo(request.BlobName));
            }

            mockQueueClient.Verify(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task QueueStorageService_EnqueueAsync_Exception_ShouldCreateFailure()
        {
            // Arrange
            var request = CreateValidRequest();

            mockQueueClient
                .Setup(c => c.SendMessageAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .Verifiable();

            // Act
            var result = await service.EnqueueAsync(request, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(ErrorCodes.QueueStorageError));
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.QueuePushUnexpectedFailed));

                var log = logger.Entries.Single(e => e.EventId.Id == 5);
                Assert.That(log.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<QueueStorageService>.GetStateValue(log, "BlobName"), Is.EqualTo(request.BlobName));
            }

            mockQueueClient.Verify(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task QueueStorageService_EnqueueAsync_ShouldReturnSuccess()
        {
            // Arrange
            var request = CreateValidRequest();
            var messageId = "msg-123";
            var insertionTime = DateTimeOffset.UtcNow;
            var response = (messageId, insertionTime);

            mockQueueClient
                .Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await service.EnqueueAsync(request, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.ErrorCode, Is.Null);
                Assert.That(result.ErrorMessage, Is.Null);
                Assert.That(result.MessageId, Is.EqualTo(messageId));
                Assert.That(result.LastModifiedUtc, Is.EqualTo(insertionTime));

                var entry = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(entry.Level, Is.EqualTo(LogLevel.Debug));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(entry, "BlobName"), Is.EqualTo(request.BlobName));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(entry, "TrackingId"), Is.EqualTo(request.TrackingId));

                var successLog = logger.Entries.Single(e => e.EventId.Id == 3);
                Assert.That(successLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(successLog, "BlobName"), Is.EqualTo(request.BlobName));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(successLog, "TrackingId"), Is.EqualTo(request.TrackingId));
            }

            mockQueueClient.Verify(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion
    }
}
