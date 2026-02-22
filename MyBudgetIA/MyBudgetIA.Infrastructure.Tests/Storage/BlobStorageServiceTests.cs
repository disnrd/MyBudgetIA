using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyBudgetIA.Application.Photo.Dtos.Blob;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Exceptions;
using MyBudgetIA.Infrastructure.Storage;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using MyBudgetIA.Infrastructure.Storage.Blob;
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
        private Mock<IAzureStorageErrorMapper> mockAzureStorageErrorMapper;

        #region SetUp

        [SetUp]
        public void Setup()
        {
            mockBlobServiceClient = new Mock<IAzureBlobServiceClient>();
            mockContainerClient = new Mock<BlobContainerClient>();
            mockBlobClient = new Mock<BlobClient>();
            logger = new TestLogger<BlobStorageService>();
            mockAzureStorageErrorMapper = new Mock<IAzureStorageErrorMapper>();

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
                mockAzureStorageErrorMapper.Object,
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
                trackingId: trackingId);
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
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.BlobRequestValidationFailed));
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
        [TestCase(404, "ContainerNotFound", ErrorCodes.BlobContainerNotFound)]
        [TestCase(404, "BlobNotFound", ErrorCodes.BlobNotFound)]
        [TestCase(404, "", ErrorCodes.BlobNotFound)]
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

            mockAzureStorageErrorMapper
                .Setup(m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.BlobUpload))
                .Returns(expectedErrorCode)
                .Verifiable();

            // Act
            var result = await service.UploadFileAsync(request, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(expectedErrorCode));
                Assert.That(result.ErrorMessage, Is.EqualTo(StorageErrorMessages.BlobUploadFailed));

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

            mockAzureStorageErrorMapper.Verify(
                m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.BlobUpload),
                Times.Once);
        }

        #endregion

        #region DownloadBlobAsync

        [Test]
        public async Task BlobStorageService_DownloadBlobAsync_EmptyBlobName_ShouldThrowException()
        {
            // Arrange
            var blobName = string.Empty;

            // Act
            var ex = Assert.ThrowsAsync<BlobStorageException>(async () =>
                await service.DownloadBlobAsync(blobName, CancellationToken.None));

            // Assert
            Assert.That(ex.Message, Is.EqualTo(StorageErrorMessages.BlobNameValidationFailed));
        }

        [Test]
        public async Task BlobStorageService_DownloadBlobAsync_SuccessfullDownload_ShouldReturnFullBlobDownloadData()
        {
            // Arrange
            var blobName = "test-blob";
            var content = "Hello, World!";
            var contentType = "text/plain";
            var contentLength = content.Length;
            var fileName = "test.txt";
            var trackingId = "tracking-123";
            var metadata = new Dictionary<string, string>
            {
                { nameof(BlobUploadRequest.FileName), fileName },
                { nameof(BlobUploadRequest.TrackingId), trackingId }
            };

            var blobDownloadDetails = BlobsModelFactory.BlobDownloadDetails(
                contentType: contentType,
                contentLength: contentLength,
                metadata: metadata);

            var blobDownloadStreamingResult = BlobsModelFactory.BlobDownloadStreamingResult(
                content: new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),
                details: blobDownloadDetails);

            var response = Response.FromValue(blobDownloadStreamingResult, Mock.Of<Response>());

            mockBlobClient
                .Setup(b => b.DownloadStreamingAsync(
                    It.IsAny<BlobDownloadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response)
                .Verifiable();

            // Act
            var result = await service.DownloadBlobAsync(blobName, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.ContentType, Is.EqualTo(contentType));
                Assert.That(result.FileName, Is.EqualTo(fileName));
                Assert.That(result.TrackingId, Is.EqualTo(trackingId));
                Assert.That(result.Metadata, Is.EqualTo(metadata));

                using var reader = new StreamReader(result.Content);
                Assert.That(reader.ReadToEnd(), Is.EqualTo(content));

                var startLog = logger.Entries.Single(e => e.EventId.Id == 6);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Debug));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(startLog, "BlobName"), Is.EqualTo(blobName));

                var successLog = logger.Entries.Single(e => e.EventId.Id == 8);
                Assert.That(successLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(successLog, "BlobName"), Is.EqualTo(blobName));

                mockBlobClient.Verify(b => b.DownloadStreamingAsync(
                    It.IsAny<BlobDownloadOptions>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);                
            }
        }

        [Test]
        public async Task BlobStorageService_DownloadBlobAsync_SuccessfullDownload_ShouldReturnBlobDownloadData()
        {
            // Arrange
            var blobName = "test-blob";
            var content = "Hello, World!";
            var contentType = "text/plain";
            var contentLength = content.Length;
            var metadata = new Dictionary<string, string>();

            var blobDownloadDetails = BlobsModelFactory.BlobDownloadDetails(
                contentType: contentType,
                contentLength: contentLength,
                metadata: metadata);

            var blobDownloadStreamingResult = BlobsModelFactory.BlobDownloadStreamingResult(
                content: new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),
                details: blobDownloadDetails);

            var response = Response.FromValue(blobDownloadStreamingResult, Mock.Of<Response>());

            mockBlobClient
                .Setup(b => b.DownloadStreamingAsync(
                    It.IsAny<BlobDownloadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response)
                .Verifiable();

            // Act
            var result = await service.DownloadBlobAsync(blobName, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.ContentType, Is.EqualTo(contentType));
                Assert.That(result.FileName, Is.Null);
                Assert.That(result.TrackingId, Is.Null);
                Assert.That(result.Metadata, Is.EqualTo(metadata));

                using var reader = new StreamReader(result.Content);
                Assert.That(reader.ReadToEnd(), Is.EqualTo(content));

                var startLog = logger.Entries.Single(e => e.EventId.Id == 6);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Debug));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(startLog, "BlobName"), Is.EqualTo(blobName));

                var successLog = logger.Entries.Single(e => e.EventId.Id == 8);
                Assert.That(successLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(successLog, "BlobName"), Is.EqualTo(blobName));

                mockBlobClient.Verify(b => b.DownloadStreamingAsync(
                    It.IsAny<BlobDownloadOptions>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [TestCase(404, ErrorCodes.BlobNotFound)]
        [TestCase(450, ErrorCodes.BlobDownloadFailed)]
        public async Task BlobStorageService_DownloadBlobAsync_RequestFailedException_ShouldThrowBlobStorageException_450(
            int status,
            string errorCode)
        {
            // Arrange
            var blobName = "test-blob";
            var statusCode = status;

            mockBlobClient
                .Setup(b => b.DownloadStreamingAsync(
                    It.IsAny<BlobDownloadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(
                    status: statusCode,
                    errorCode: "BlobNotFound",
                    message: "Blob not found",
                    innerException: null))
                .Verifiable();

            mockAzureStorageErrorMapper
                .Setup(m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.BlobDownload))
                .Returns(errorCode)
                .Verifiable();

            // Act
            var ex = Assert.ThrowsAsync<BlobStorageException>(async () =>
                await service.DownloadBlobAsync(blobName, CancellationToken.None));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Message, Is.EqualTo(StorageErrorMessages.BlobDownloadFailed));
                Assert.That(ex.ErrorCode, Is.EqualTo(errorCode));

                var errorLog = logger.Entries.Single(e => e.EventId.Id == 7);
                Assert.That(errorLog.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "BlobName"), Is.EqualTo(blobName));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "Status"), Is.EqualTo(statusCode));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "ErrorCode"), Is.EqualTo(errorCode));

                mockBlobClient.Verify(b => b.DownloadStreamingAsync(
                    It.IsAny<BlobDownloadOptions>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Test]
        public async Task BlobStorageService_DownloadBlobAsync_RequestFailedException_ShouldThrowBlobStorageException()
        {
            // Arrange
            var blobName = "test-blob";
            var errorCode = "BlobNotFound";
            var statusCode = 404;

            mockBlobClient
                .Setup(b => b.DownloadStreamingAsync(
                    It.IsAny<BlobDownloadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(
                    status: statusCode,
                    errorCode: errorCode,
                    message: "Blob not found",
                    innerException: null))
                .Verifiable();

            mockAzureStorageErrorMapper
                .Setup(m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.BlobDownload))
                .Returns(ErrorCodes.BlobNotFound)
                .Verifiable();

            // Act
            var ex = Assert.ThrowsAsync<BlobStorageException>(async () =>
                await service.DownloadBlobAsync(blobName, CancellationToken.None));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Message, Is.EqualTo(StorageErrorMessages.BlobDownloadFailed));
                Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCodes.BlobNotFound));

                var errorLog = logger.Entries.Single(e => e.EventId.Id == 7);
                Assert.That(errorLog.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "BlobName"), Is.EqualTo(blobName));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "Status"), Is.EqualTo(statusCode));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "ErrorCode"), Is.EqualTo(ErrorCodes.BlobNotFound));

                mockBlobClient.Verify(b => b.DownloadStreamingAsync(
                    It.IsAny<BlobDownloadOptions>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                mockAzureStorageErrorMapper.Verify(
                    m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.BlobDownload),
                    Times.Once);
            }

        }

        #endregion

        #region GetBlobsInfoAsync

        [Test]
        public async Task BlobStorageService_GetBlobsInfoAsync_ShouldReturnListOfBlobInfo()
        {
            // Arrange
            var blobName1 = "blob1";
            var fileName1 = "test1.jpg";
            var lastUpdated1 = DateTimeOffset.UtcNow.AddDays(-1);
            var blob1 = BlobsModelFactory
                .BlobItem(
                    name: blobName1,
                    metadata: new Dictionary<string, string> { { nameof(BlobData.FileName), fileName1 } },
                    properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: false, lastModified: lastUpdated1));

            var blobName2 = "blob2";
            var fileName2 = "test2.jpg";
            var lastUpdated2 = DateTimeOffset.UtcNow;
            var blob2 = BlobsModelFactory
                .BlobItem(
                    name: blobName2,
                    metadata: new Dictionary<string, string> { { nameof(BlobData.FileName), fileName2 } },
                    properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: false, lastModified: lastUpdated2));

            var blobs = new[] { blob1, blob2 };

            var asyncPageable = BlobAsyncPageableFactory.Create(blobs);

            mockContainerClient
                .Setup(c => c.GetBlobsAsync(
                    It.IsAny<BlobTraits>(),
                    It.IsAny<BlobStates>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(asyncPageable)
                .Verifiable();

            // Act
            var result = await service.GetBlobsInfoAsync(CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count(), Is.EqualTo(2));

                var list = result.ToList();
                Assert.That(list[0].BlobName, Is.EqualTo(blobName1));
                Assert.That(list[0].FileName, Is.EqualTo(fileName1));
                Assert.That(list[0].LastModified, Is.EqualTo(lastUpdated1));

                Assert.That(list[1].BlobName, Is.EqualTo(blobName2));
                Assert.That(list[1].FileName, Is.EqualTo(fileName2));
                Assert.That(list[1].LastModified, Is.EqualTo(lastUpdated2));

                var startLog = logger.Entries.Single(e => e.EventId.Id == 9);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Debug));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(startLog, "ContainerName"), Is.EqualTo(ContainerName));

                var successLog = logger.Entries.Single(e => e.EventId.Id == 11);
                Assert.That(successLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(successLog, "ContainerName"), Is.EqualTo(ContainerName));

                mockContainerClient.Verify(c => c.GetBlobsAsync(
                    It.IsAny<BlobTraits>(),
                    It.IsAny<BlobStates>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        public static class BlobAsyncPageableFactory
        {
            public static AsyncPageable<BlobItem> Create(IEnumerable<BlobItem> items)
            {
                var page = Page<BlobItem>.FromValues([.. items], null, null!);
                return AsyncPageable<BlobItem>.FromPages([page]);
            }
        }

        [Test]
        public async Task BlobStorageService_GetBlobsInfoAsync_RequestFailedException_ShouldThrowBlobStorageException()
        {
            // Arrange
            var statusCode = 404;

            mockContainerClient
                .Setup(c => c.GetBlobsAsync(
                    It.IsAny<BlobTraits>(),
                    It.IsAny<BlobStates>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Throws(new RequestFailedException(
                    status: statusCode,
                    errorCode: "BlobNotFound",
                    message: "Blob not found",
                    innerException: null))
                .Verifiable();

            mockAzureStorageErrorMapper
                .Setup(m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.BlobDownload))
                .Returns(ErrorCodes.BlobNotFound)
                .Verifiable();

            // Act
            var ex = Assert.ThrowsAsync<BlobStorageException>(async () =>
                await service.GetBlobsInfoAsync(CancellationToken.None));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Message, Is.EqualTo(StorageErrorMessages.BlobsListFailed));
                Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCodes.BlobNotFound));

                var errorLog = logger.Entries.Single(e => e.EventId.Id == 10);
                Assert.That(errorLog.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "ContainerName"), Is.EqualTo(ContainerName));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "Status"), Is.EqualTo(statusCode));
                Assert.That(TestLogger<BlobStorageService>.GetStateValue(errorLog, "ErrorCode"), Is.EqualTo(ErrorCodes.BlobNotFound));

                mockContainerClient.Verify(b => b.GetBlobsAsync(
                    It.IsAny<BlobTraits>(),
                    It.IsAny<BlobStates>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                mockAzureStorageErrorMapper.Verify(
                    m => m.Map(It.IsAny<RequestFailedException>(), StorageOperationType.BlobDownload),
                    Times.Once);
            }
        }

        #endregion
    }
}
