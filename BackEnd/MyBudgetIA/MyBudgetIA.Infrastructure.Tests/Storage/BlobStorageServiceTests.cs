using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Storage;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using Shared.Models;
using Shared.TestsLogging;

namespace MyBudgetIA.Infrastructure.Tests.Storage
{
    /// <summary>
    /// Unit tests for the <see cref="BlobStorageService"/> class.
    /// </summary>
    [TestFixture]
    public class BlobStorageServiceTests
    {
        private const string ContainerName = "test-container";

        private Mock<IAzureBlobServiceClient> mockBlobServiceClient;
        private Mock<BlobContainerClient> mockContainerClient;
        private Mock<BlobClient> mockBlobClient;
        private IOptions<BlobStorageSettings> options;
        private TestLogger<BlobStorageService> logger;
        private BlobStorageService service;

        #region SetUp

        [SetUp]
        public void Setup()
        {
            mockBlobServiceClient = new Mock<IAzureBlobServiceClient>();
            mockContainerClient = new Mock<BlobContainerClient>();
            mockBlobClient = new Mock<BlobClient>();
            logger = new TestLogger<BlobStorageService>();


            options = Options.Create(new BlobStorageSettings
            {
                ContainerName = ContainerName
            });

            mockBlobServiceClient
                .Setup(x => x.GetBlobContainerClient(ContainerName))
                .Returns(mockContainerClient.Object);

            mockContainerClient
                .SetupGet(c => c.Name)
                .Returns(ContainerName);

            mockContainerClient
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(mockBlobClient.Object);

            service = new BlobStorageService(
                mockBlobServiceClient.Object,
                options,
                logger);
        }

        #endregion

        #region UploadFileAsync

        private static BlobUploadRequest CreateValidRequest()
        {
            var fileName = "test.txt";
            var blobName = "test-blob";
            var trackingId = Guid.NewGuid().ToString("N");

            var stream = new MemoryStream("abc"u8.ToArray());

            return new BlobUploadRequest(
                fileName: fileName,
                blobName: blobName,
                stream: stream,
                contentType: "text/plain",
                trackingId: trackingId)
            {
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };
        }

        [Test]
        public async Task BlobStorageService_UploadFileAsync_Should_Return_Success_When_Upload_Succeeds()
        {
            // Arrange
            var request = CreateValidRequest();

            var info = BlobsModelFactory.BlobContentInfo(
                eTag: new ETag("\"etag\""),
                lastModified: DateTimeOffset.UtcNow,
                contentHash: null,
                encryptionKeySha256: null,
                blobSequenceNumber: 0);

            var response = Response.FromValue(info, Mock.Of<Response>());

            mockBlobClient
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response)
                .Verifiable();

            // Act
            var result = await service.UploadFileAsync(request, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.ErrorCode, Is.Null);
                Assert.That(result.ErrorMessage, Is.Null);

                var entry = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(entry.Level, Is.EqualTo(LogLevel.Debug));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(entry, "BlobName"), Is.EqualTo(request.BlobName));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(entry, "ContainerName"), Is.EqualTo(ContainerName));

                var successLog = logger.Entries.Single(e => e.EventId.Id == 3);
                Assert.That(successLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(successLog, "BlobName"), Is.EqualTo(request.BlobName));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(successLog, "TrackingId"), Is.EqualTo(request.TrackingId));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(successLog, "ContainerName"), Is.EqualTo(ContainerName));
            }

            mockBlobClient.Verify(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task BlobStorageService_UploadFileAsync_InvalidRequest_Should_Return_Failure()
        {
            // Arrange
            var request = new BlobUploadRequest(
                fileName: "",
                blobName: "",
                stream: null!,
                contentType: "",

                trackingId: "");
            // Act
            var result = await service.UploadFileAsync(request, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(ErrorCodes.BlobValidationFailed));
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.ValidationFailed));
                var log = logger.Entries.Single(e => e.EventId.Id == 2);
                Assert.That(log.Level, Is.EqualTo(LogLevel.Warning));
            }
        }

        [Test]
        public async Task BlobStorageService_UploadFileAsync_StreamPositionNotZero_Should_Reset_To_Zero()
        {
            // Arrange
            var request = CreateValidRequest();

            // Force a non-zero position on a seekable stream (MemoryStream is seekable)
            request.Stream.Position = 2;
            Assert.That(request.Stream.Position, Is.Not.Zero);

            var info = BlobsModelFactory.BlobContentInfo(
                eTag: new ETag("\"etag\""),
                lastModified: DateTimeOffset.UtcNow,
                contentHash: null,
                encryptionKeySha256: null,
                blobSequenceNumber: 0);

            var response = Response.FromValue(info, Mock.Of<Response>());

            mockBlobClient
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await service.UploadFileAsync(request, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(request.Stream.Position, Is.Zero);
            }
        }

        [Test]
        public async Task BlobStorageService_UploadFileAsync_ThrowNormalException_ShouldReturnFailure()
        {
            // Arrange
            var request = CreateValidRequest();

            mockBlobClient
                .Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .Verifiable();

            // Act
            var result = await service.UploadFileAsync(request, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(ErrorCodes.BlobStorageError));
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.UnexpectedUploadFailure));

                var errorLog = logger.Entries.Single(e => e.EventId.Id == 5);
                Assert.That(errorLog.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "BlobName"), Is.EqualTo(request.BlobName));
            }

            mockBlobClient.Verify(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestCase(403, "", ErrorCodes.BlobUnauthorized)]
        [TestCase(404, "", ErrorCodes.BlobContainerNotFound)]
        [TestCase(405, "ContainerNotFound", ErrorCodes.BlobContainerNotFound)]
        [TestCase(409, "", ErrorCodes.BlobAlreadyExists)]
        [TestCase(410, "BlobAlreadyExists", ErrorCodes.BlobAlreadyExists)]
        [TestCase(429, "", ErrorCodes.BlobThrottled)]
        [TestCase(450, "", ErrorCodes.BlobUploadFailed)] // <-- to cover unmapped status code with specific error code
        [TestCase(503, "", ErrorCodes.BlobUnavailable)]
        public async Task BlobStorageService_UploadFileAsync_ThrowRequestFailedException_Should_Map_To_Domain_ErrorCode(
            int status,
            string azureErrorCode,
            string expectedErrorCode)
        {
            // Arrange
            var request = CreateValidRequest();

            mockBlobClient
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(
                    status: status,
                    errorCode: azureErrorCode,
                    message: "failed",
                    innerException: null));

            // Act
            var result = await service.UploadFileAsync(request, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(expectedErrorCode));
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.AzureBlobUploadFailed));

                var log = logger.Entries.Single(e => e.EventId.Id == 4);
                Assert.That(log.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(log, "BlobName"), Is.EqualTo(request.BlobName));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(log, "Status"), Is.EqualTo(status));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(log, "ErrorCode"), Is.EqualTo(expectedErrorCode));
            }

            mockBlobClient.Verify(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion
    }
}
