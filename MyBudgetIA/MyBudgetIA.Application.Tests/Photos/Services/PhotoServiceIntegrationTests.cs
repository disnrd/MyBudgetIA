using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Application.Photo.Dtos.Blob;
using MyBudgetIA.Application.Photo.Dtos.Queue;
using MyBudgetIA.Application.TechnicalServices;
using MyBudgetIA.Infrastructure.Configuration;
using MyBudgetIA.Infrastructure.Services;
using MyBudgetIA.Infrastructure.Storage.Abstractions;
using MyBudgetIA.Infrastructure.Storage.Abstractions.ErrorMapper;
using MyBudgetIA.Infrastructure.Storage.Blob;
using MyBudgetIA.Infrastructure.Storage.Queue;
using Shared.Models;
using Shared.Storage.DTOS;
using System.Text;
using System.Text.Json;

namespace MyBudgetIA.Application.Tests.Photos.Services
{
    /// <summary>
    /// Integration tests for the <see cref="PhotoService"/> class.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [NonParallelizable] // évite les effets de bord Azurite, garantit l'isolation.
    public class PhotoServiceIntegrationTests
    {
        private const string ConnectionString = "UseDevelopmentStorage=true";

        private ServiceProvider? serviceProvider;

        private string containerName = default!;
        private string queueName = default!;

        private BlobContainerClient? containerClient;
        private QueueClient? queueClient;

        private PhotoService photoService;
        private IValidationService validationService;
        private IStreamValidationService streamValidationService;
        private IQueueStorageService queueStorageService;
        private BlobStorageService blobStorageService;

        #region SetUp and TearDown

        [SetUp]
        public async Task SetUp()
        {
            var services = new ServiceCollection();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IStreamValidationService, StreamValidationService>();
            services.AddValidatorsFromAssemblyContaining<AssemblyMarker>();

            serviceProvider = services.BuildServiceProvider();
            validationService = serviceProvider.GetRequiredService<IValidationService>();
            streamValidationService = serviceProvider.GetRequiredService<IStreamValidationService>();

            containerName = $"photos-it-{Guid.NewGuid():N}".ToLowerInvariant();
            queueName = $"photos-queue-it-{Guid.NewGuid():N}".ToLowerInvariant();

            IAzureStorageErrorMapper azureStorageErrorMapper = new AzureStorageErrorMapper(
                new AzureStorageErrorMapperFactory(
                    new AzureBlobErrorMapper(),
                    new AzureQueueErrorMapper()));

            var blobServiceClient = new BlobServiceClient(ConnectionString);
            containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobOptions = Options.Create(new BlobStorageSettings
            {
                ConnectionString = ConnectionString,
                ContainerName = containerName
            });

            blobStorageService = new BlobStorageService(
                blobService: new AzureBlobServiceClientAdapter(blobServiceClient),
                options: blobOptions,
                azureStorageErrorMapper: azureStorageErrorMapper,
                logger: NullLogger<BlobStorageService>.Instance);

            queueClient = new QueueClient(ConnectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.ClearMessagesAsync();

            queueStorageService = new QueueStorageService(
                queueServiceClient: new AzureQueueServiceClientAdapter(queueClient),
                azureStorageErrorMapper: azureStorageErrorMapper,
                logger: NullLogger<QueueStorageService>.Instance);

            photoService = new PhotoService(
                validationService,
                streamValidationService,
                blobStorageService,
                queueStorageService,
                logger: NullLogger<PhotoService>.Instance);
        }

        [TearDown]
        public async Task TearDown()
        {
            if (queueClient is not null)
            {
                await queueClient.DeleteIfExistsAsync();
            }

            if (containerClient is not null)
            {
                await containerClient.DeleteIfExistsAsync();
            }

            serviceProvider?.Dispose();
        }

        #endregion

        #region UploadPhotoAsync

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private sealed class InMemoryFileUploadRequest(string fileName, string contentType, byte[] bytes) : IFileUploadRequest
        {
            private readonly byte[] bytes = bytes;

            public string FileName { get; } = fileName;
            public string ContentType { get; } = contentType;
            public long Length { get; } = bytes.LongLength;
            public string Extension { get; } = Path.GetExtension(fileName);

            public Stream OpenReadStream() => new MemoryStream(bytes, writable: false);
        }

        private sealed class DeleteQueueBeforeSecondEnqueueQueueStorageService(
            IQueueStorageService inner,
            QueueClient queueClient) : IQueueStorageService
        {
            private int callCount;

            public async Task<QueuePushResult> EnqueueAsync(QueueMessageRequest request, CancellationToken ct = default)
            {
                var call = Interlocked.Increment(ref callCount);

                // 1ère photo => queue OK, 2ème photo => on supprime la queue juste avant l'envoi => 404 QueueNotFound
                if (call == 2)
                {
                    await queueClient.DeleteIfExistsAsync(ct);
                }

                return await inner.EnqueueAsync(request, ct);
            }
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenOneValidPhoto_ShouldUploadBlob_AndEnqueueMessage_AndReturnSuccess()
        {
            // Arrange
            var ct = CancellationToken.None;
            var bytes = "hello"u8.ToArray();

            IFileUploadRequest photo = new InMemoryFileUploadRequest(
                fileName: "photo.png",
                contentType: "image/png",
                bytes: bytes);

            // Act
            var (results, message) = await photoService.UploadPhotoAsync([photo], ct);

            // Assert
            var result = results.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Is.EqualTo(PhotoService.Messages.SuccesMessage.AllPhotosUploadedSuccessfully));

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.BlobResult.IsSuccess, Is.True);
                Assert.That(result.QueueResult.IsSuccess, Is.True);

                Assert.That(result.BlobName, Is.Not.Null.And.Not.Empty);
                Assert.That(result.BlobName, Does.StartWith("photos/"));
                Assert.That(result.TrackingId, Is.Not.Null.And.Not.Empty);
                Assert.That(result.FileName, Is.EqualTo(photo.FileName));
                Assert.That(result.ContentType, Is.EqualTo(photo.ContentType));
            }

            var blobClient = containerClient!.GetBlobClient(result.BlobName);
            var existsResponse = await blobClient.ExistsAsync(ct);
            Assert.That(existsResponse.Value, Is.True);

            var props = await blobClient.GetPropertiesAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(props.Value.ContentType, Is.EqualTo(photo.ContentType));
                Assert.That(props.Value.Metadata["FileName"], Is.EqualTo(photo.FileName));
                Assert.That(props.Value.Metadata["TrackingId"], Is.EqualTo(result.TrackingId));
            }

            // Assert (queue state)
            var received = await queueClient!.ReceiveMessageAsync(cancellationToken: ct);
            Assert.That(received.Value, Is.Not.Null, "Aucun message reçu depuis la queue (Azurite).");

            var body = received.Value.Body.ToString();
            var payload = JsonSerializer.Deserialize<QueueMessageRequest>(body, JsonOptions);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(payload, Is.Not.Null);
                Assert.That(payload!.BlobName, Is.EqualTo(result.BlobName));
                Assert.That(payload.TrackingId, Is.EqualTo(result.TrackingId));
            }

            await queueClient.DeleteMessageAsync(received.Value.MessageId, received.Value.PopReceipt, ct);
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenTwoPhotos_OneQueueOk_OneQueueKo_ShouldReturnPartialSuccessMessage()
        {
            // Arrange
            var ct = CancellationToken.None;

            IFileUploadRequest photo1 = new InMemoryFileUploadRequest(
                fileName: "photo1.png",
                contentType: "image/png",
                bytes: "one"u8.ToArray());

            IFileUploadRequest photo2 = new InMemoryFileUploadRequest(
                fileName: "photo2.png",
                contentType: "image/png",
                bytes: "two"u8.ToArray());

            var flakyQueue = new DeleteQueueBeforeSecondEnqueueQueueStorageService(queueStorageService, queueClient!);

            var photoServiceWithFlakyQueue = new PhotoService(
                validationService,
                streamValidationService,
                blobStorageService,
                flakyQueue,
                logger: NullLogger<PhotoService>.Instance);

            // Act
            var (results, message) = await photoServiceWithFlakyQueue.UploadPhotoAsync([photo1, photo2], ct);

            // Assert
            var list = results.ToList();
            var r1 = list.Single(r => r.FileName == photo1.FileName);
            var r2 = list.Single(r => r.FileName == photo2.FileName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list, Has.Count.EqualTo(2));
                Assert.That(message, Is.EqualTo(PhotoService.Messages.SuccesMessage.SomePhotosUploadedSuccessfully));

                // Photo 1 => OK complet
                Assert.That(r1.BlobResult.IsSuccess, Is.True);
                Assert.That(r1.QueueResult.IsSuccess, Is.True);
                Assert.That(r1.IsSuccess, Is.True);

                // Photo 2 => blob OK mais queue KO
                Assert.That(r2.BlobResult.IsSuccess, Is.True);
                Assert.That(r2.QueueResult.IsSuccess, Is.False);
                Assert.That(r2.QueueResult.ErrorCode, Is.EqualTo(ErrorCodes.QueueNotFound));
                Assert.That(r2.IsSuccess, Is.False);

                Assert.That((await containerClient!.GetBlobClient(r1.BlobName).ExistsAsync(ct)).Value, Is.True);
                Assert.That((await containerClient!.GetBlobClient(r2.BlobName).ExistsAsync(ct)).Value, Is.True);
            }
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenQueueIsMissing_ShouldUploadBlob_ButReturnFailure()
        {
            // Arrange
            var ct = CancellationToken.None;

            // Force un échec réel de queue (404 QueueNotFound) en supprimant la queue avant l'appel
            await queueClient!.DeleteIfExistsAsync(ct);

            var bytes = "hello"u8.ToArray();

            IFileUploadRequest photo = new InMemoryFileUploadRequest(
                fileName: "photo.png",
                contentType: "image/png",
                bytes: bytes);

            // Act
            var (results, message) = await photoService.UploadPhotoAsync([photo], ct);

            // Assert
            var result = results.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Is.EqualTo(PhotoService.Messages.SuccesMessage.FailedToUploadAllPhotos));

                Assert.That(result.BlobResult.IsSuccess, Is.True);
                Assert.That(result.QueueResult.IsSuccess, Is.False);
                Assert.That(result.QueueResult.ErrorCode, Is.EqualTo(ErrorCodes.QueueNotFound));

                Assert.That(result.IsSuccess, Is.False);
            }

            var blobClient = containerClient!.GetBlobClient(result.BlobName);
            Assert.That((await blobClient.ExistsAsync(ct)).Value, Is.True);
        }

        #endregion

        #region DownloadPhotoAsync

        [Test]
        public async Task PhotoService_DownloadPhotoAsync_WhenBlobExists_ShouldReturnContent()
        {
            // Arrange
            var ct = CancellationToken.None;
            var blobName = $"photos/{Guid.NewGuid():N}.txt";
            const string fileName = "a.txt";
            const string contentType = "text/plain";
            var trackingId = Guid.NewGuid().ToString("N");
            const string payload = "hello-from-integration-test";

            await using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            var uploadRequest = new BlobUploadRequest(
                fileName: fileName,
                blobName: blobName,
                stream: uploadStream,
                contentType: contentType,
                trackingId: trackingId);

            var uploadResult = await blobStorageService.UploadFileAsync(uploadRequest, ct);
            Assert.That(uploadResult.IsSuccess, Is.True, uploadResult.ErrorMessage);

            // Act
            var downloaded = await photoService.DownloadPhotoAsync(blobName, ct);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(downloaded, Is.Not.Null);
                Assert.That(downloaded.Content, Is.Not.Null);
                Assert.That(downloaded.ContentType, Is.EqualTo(contentType));
                Assert.That(downloaded.FileName, Is.EqualTo(fileName));

                using var reader = new StreamReader(downloaded.Content, Encoding.UTF8, leaveOpen: true);
                var content = await reader.ReadToEndAsync(ct);
                Assert.That(content, Is.EqualTo(payload));
            }
        }

        #endregion

        #region GetUploadedPhotosInfosAsync

        [Test]
        public async Task PhotoService_GetUploadedPhotosInfosAsync_WhenTwoBlobsExist_ShouldReturnTwoItems()
        {
            // Arrange
            var ct = CancellationToken.None;

            var blobName1 = $"photos/{Guid.NewGuid():N}-1.txt";
            var blobName2 = $"photos/{Guid.NewGuid():N}-2.txt";

            await using (var s1 = new MemoryStream("one"u8.ToArray()))
            {
                var r1 = new BlobUploadRequest("one.txt", blobName1, s1, "text/plain", Guid.NewGuid().ToString("N"));
                var res1 = await blobStorageService.UploadFileAsync(r1, ct);
                Assert.That(res1.IsSuccess, Is.True, res1.ErrorMessage);
            }

            await using (var s2 = new MemoryStream("two"u8.ToArray()))
            {
                var r2 = new BlobUploadRequest("two.txt", blobName2, s2, "text/plain", Guid.NewGuid().ToString("N"));
                var res2 = await blobStorageService.UploadFileAsync(r2, ct);
                Assert.That(res2.IsSuccess, Is.True, res2.ErrorMessage);
            }

            // Act
            var infos = (await photoService.GetUploadedPhotosInfosAsync(ct)).ToList();

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(infos, Has.Count.EqualTo(2));
                Assert.That(infos.Select(i => i.BlobName).ToHashSet(), Is.EquivalentTo([blobName1, blobName2]));
                Assert.That(infos.Select(i => i.FileName).ToHashSet(), Is.EquivalentTo(["one.txt", "two.txt"]));
                Assert.That(infos.All(i => i.LastModified is not null), Is.True);
            }
        }

        #endregion
    }
}