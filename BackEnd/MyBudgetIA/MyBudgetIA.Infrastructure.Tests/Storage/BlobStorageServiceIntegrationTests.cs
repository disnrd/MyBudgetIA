//using Azure.Storage.Blobs;
//using Microsoft.Extensions.Logging.Abstractions;
//using Microsoft.Extensions.Options;
//using MyBudgetIA.Application.Photo.Dtos;
//using MyBudgetIA.Infrastructure.Configuration;
//using MyBudgetIA.Infrastructure.Storage;
//using MyBudgetIA.Infrastructure.Storage.Abstractions;

//namespace MyBudgetIA.Infrastructure.Tests.Storage
//{
//    /// <summary>
//    /// Integration tests for the <see cref="BlobStorageService"/> class.
//    /// </summary>
//    [TestFixture]
//    [Category("Integration")]
//    public class BlobStorageServiceIntegrationTests
//    {
//        private const string ConnectionString = "UseDevelopmentStorage=true";
//        private const string ContainerName = "photos-integration-tests";

//        private BlobServiceClient blobServiceClient;
//        private BlobContainerClient containerClient;
//        private BlobStorageService blobStorageService;

//        #region SetUp and TearDown

//        [SetUp]
//        public async Task SetUp()
//        {
//            blobServiceClient = new BlobServiceClient(ConnectionString);
//            containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

//            await containerClient.CreateIfNotExistsAsync();

//            var options = Options.Create(new BlobStorageSettings
//            {
//                ContainerName = ContainerName,
//                ConnectionString = ConnectionString
//            });

//            var adapter = new AzureBlobServiceClientAdapter(blobServiceClient);

//            blobStorageService = new BlobStorageService(
//                blobService: adapter,
//                options: options,
//                logger: NullLogger<BlobStorageService>.Instance);
//        }

//        [TearDown]
//        public async Task TearDown()
//        {
//            // Keep or not? 
//            // await containerClient.DeleteIfExistsAsync();
//            await Task.CompletedTask;
//        }

//        #endregion

//        [Test]
//        public async Task BlobStorageService_UploadFileAsync_ShouldUpload_AndSetHeaders_AndMetadata()
//        {
//            // Arrange
//            var blobName = $"photos/{Guid.NewGuid():N}.png";
//            var trackingId = Guid.NewGuid().ToString("N");
//            const string fileName = "a.png";
//            const string contentType = "image/png";

//            await using var stream = new MemoryStream("hello"u8.ToArray());

//            var request = new BlobUploadRequest(
//                fileName: fileName,
//                blobName: blobName,
//                stream: stream,
//                contentType: contentType,
//                trackingId: trackingId)
//            {
//                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
//            };

//            // Act
//            var result = await blobStorageService.UploadFileAsync(request, CancellationToken.None);

//            // Assert (service result)
//            using (Assert.EnterMultipleScope())
//            {
//                Assert.That(result.IsSuccess, Is.True);
//                Assert.That(result.FileName, Is.EqualTo(fileName));
//                Assert.That(result.BlobName, Is.EqualTo(blobName));
//                Assert.That(result.TrackingId, Is.EqualTo(trackingId));
//                Assert.That(result.Etag, Is.Not.Null.And.Not.Empty);
//            }

//            // Assert (storage state)
//            var uploaded = containerClient.GetBlobClient(blobName);
//            var props = await uploaded.GetPropertiesAsync();

//            using (Assert.EnterMultipleScope())
//            {
//                Assert.That(result.Etag, Is.EqualTo(props.Value.ETag.ToString()));

//                Assert.That(props.Value.ContentType, Is.EqualTo(contentType));
//                Assert.That(props.Value.Metadata.ContainsKey("contentType"), Is.True);
//                Assert.That(props.Value.Metadata["contentType"], Is.EqualTo(contentType));

//                Assert.That(props.Value.Metadata.ContainsKey("trackingId"), Is.True);
//                Assert.That(props.Value.Metadata["trackingId"], Is.EqualTo(trackingId));
//            }
//        }

//        [Test]
//        public async Task BlobStorageService_UploadFileAsync_WhenBlobAlreadyExists_ShouldReturnBlobAlreadyExists_409()
//        {
//            // Arrange
//            var blobName = $"photos/{Guid.NewGuid():N}.txt";

//            // 409 ? Azure Blob Storage returns 409 Conflict when trying to upload a blob that already exists without overwrite flag.
//            await using var s1 = new MemoryStream("one"u8.ToArray());
//            var r1 = new BlobUploadRequest("a.txt", blobName, s1, "text/plain", Guid.NewGuid().ToString("N"))
//            {
//                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
//            };

//            await using var s2 = new MemoryStream("two"u8.ToArray());
//            var r2 = new BlobUploadRequest("a.txt", blobName, s2, "text/plain", Guid.NewGuid().ToString("N"))
//            {
//                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
//            };

//            // Act
//            var first = await blobStorageService.UploadFileAsync(r1, CancellationToken.None);
//            var second = await blobStorageService.UploadFileAsync(r2, CancellationToken.None);

//            // Assert
//            using (Assert.EnterMultipleScope())
//            {
//                Assert.That(first.IsSuccess, Is.True);

//                Assert.That(second.IsSuccess, Is.False);
//                Assert.That(second.ErrorCode, Is.EqualTo(Shared.Models.ErrorCodes.BlobAlreadyExists));
//                Assert.That(second.ErrorMessage, Is.EqualTo(StorageErrorMessages.AzureBlobUploadFailed));
//            }
//        }
//    }
//}
