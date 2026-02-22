using Microsoft.Extensions.Logging;
using Moq;
using MyBudgetIA.Application.Exceptions;
using MyBudgetIA.Application.Interfaces;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Application.Photo.Dtos.Blob;
using MyBudgetIA.Application.Photo.Dtos.Queue;
using MyBudgetIA.Application.TechnicalServices;
using MyBudgetIA.Domain.Constraints;
using Shared.Models;
using Shared.Storage.DTOS;
using Shared.TestsLogging;
using Messages = MyBudgetIA.Application.Photo.PhotoService.Messages;
using StreamMessages = MyBudgetIA.Application.TechnicalServices.StreamValidationService.Messages;

namespace MyBudgetIA.Application.Tests.Photos.Services
{
    /// <summary>
    /// Unit tests for the <see cref="PhotoService"/> class.
    /// </summary>
    [TestFixture]
    class PhotoServiceTests
    {
        private Mock<IValidationService> mockValidationService;
        private Mock<IStreamValidationService> mockStreamValidationService;
        private Mock<IBlobStorageService> mockBlobStorageService;
        private Mock<IQueueStorageService> mockQueueStorageService;
        private TestLogger<PhotoService> logger;
        private PhotoService photoService;

        #region SetUp

        [SetUp]
        public void Setup()
        {
            mockValidationService = new Mock<IValidationService>(MockBehavior.Strict);
            mockStreamValidationService = new Mock<IStreamValidationService>(MockBehavior.Strict);
            mockBlobStorageService = new Mock<IBlobStorageService>(MockBehavior.Strict);
            mockQueueStorageService = new Mock<IQueueStorageService>(MockBehavior.Strict);

            logger = new TestLogger<PhotoService>();

            photoService = new PhotoService(
                mockValidationService.Object,
                mockStreamValidationService.Object,
                mockBlobStorageService.Object,
                mockQueueStorageService.Object,
                logger);
        }

        #endregion

        #region UploadPhotoAsync

        private static IFileUploadRequest CreateValidPhoto(int count)
        {
            string fileName = $"photo{count}.jpg";
            const string contentType = "image/jog";
            const string extension = ".jpg";
            var stream = new MemoryStream("hello"u8.ToArray());
            long length = stream.Length;

            var mockPhoto = new Mock<IFileUploadRequest>();
            mockPhoto.SetupGet(p => p.FileName).Returns(fileName);
            mockPhoto.SetupGet(p => p.ContentType).Returns(contentType);
            mockPhoto.SetupGet(p => p.Extension).Returns(extension);
            mockPhoto.Setup(p => p.OpenReadStream()).Returns(stream);
            mockPhoto.SetupGet(p => p.Length).Returns(length);

            return mockPhoto.Object;
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenTooManyPhotos_ShouldThrowAndLog()
        {
            // Arrange
            var photos = new List<IFileUploadRequest>();

            for (int i = 0; i < PhotoConstraints.MaxPhotosPerRequest + 1; i++)
            {
                var mockPhoto = new Mock<IFileUploadRequest>();
                mockPhoto.SetupGet(p => p.FileName).Returns($"photo{i}.jpg");
                photos.Add(mockPhoto.Object);
            }

            // Act & Assert
            _ = Assert.ThrowsAsync<MaxPhotoCountExceptions>(()
                => photoService.UploadPhotoAsync(photos, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                var startLog = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(startLog, "Count"), Is.EqualTo(photos.Count));

                var errorlog = logger.Entries.Single(e => e.EventId.Id == 2);
                Assert.That(errorlog.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<PhotoService>.GetStateValue(errorlog, "MaxPhotosCount"), Is.EqualTo(PhotoConstraints.MaxPhotosPerRequest));
                Assert.That(TestLogger<PhotoService>.GetStateValue(errorlog, "ProvidedPhotosCount"), Is.EqualTo(photos.Count));
            }

            mockValidationService.VerifyNoOtherCalls();
            mockBlobStorageService.VerifyNoOtherCalls();
            mockQueueStorageService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenRequestValidationFail_ShouldReturnValidationFailure()
        {
            // Arrange
            const string fileName = "photo.jpg";
            var mockPhoto = new Mock<IFileUploadRequest>();
            mockPhoto.SetupGet(p => p.FileName).Returns(fileName);
            var photos = new List<IFileUploadRequest> { mockPhoto.Object };
            var errorMessage = "Validation failed for the photo.";

            mockValidationService
                .Setup(s => s.ValidateAndThrowAllAsync(
                    It.IsAny<IFileUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["Photo"] = [errorMessage]
                    }))
                .Verifiable();

            // Act
            var (results, message) = await photoService.UploadPhotoAsync(photos, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Is.Not.Null);
                Assert.That(results.Count(), Is.EqualTo(1));
                var result = results.Single();
                Assert.That(result, Is.Not.Null);
                Assert.That(message, Is.EqualTo(Messages.SuccesMessage.FailedToUploadAllPhotos));
                Assert.That(result.IsSuccess, Is.False);

                var startLog = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(startLog, "Count"), Is.EqualTo(photos.Count));

                var validationLog = logger.Entries.Single(e => e.EventId.Id == 6);
                Assert.That(validationLog.Level, Is.EqualTo(LogLevel.Warning));
                Assert.That(TestLogger<PhotoService>.GetStateValue(validationLog, "FileName"), Is.EqualTo(mockPhoto.Object.FileName));
                var loggedMessages = (IReadOnlyCollection<string>)TestLogger<PhotoService>.GetStateValue(validationLog, "Messages")!;
                Assert.That(loggedMessages.Single(), Is.EqualTo(errorMessage));
            }

            mockValidationService.Verify(s => s.ValidateAndThrowAllAsync(mockPhoto.Object, It.IsAny<CancellationToken>()), Times.Once);
            mockStreamValidationService.VerifyNoOtherCalls();
            mockBlobStorageService.VerifyNoOtherCalls();
            mockQueueStorageService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenStreamValidationFails_ShouldReturnValidationFailure()
        {
            // Arrange
            const string fileName = "photo.jpg";
            var mockPhoto = new Mock<IFileUploadRequest>();
            mockPhoto.SetupGet(p => p.FileName).Returns(fileName);
            var photos = new List<IFileUploadRequest> { mockPhoto.Object };
            var errorMessage = StreamMessages.StreamValidation.StreamLengthMustMatchProvidedLength;


            mockValidationService
                .Setup(s => s.ValidateAndThrowAllAsync(
                    It.IsAny<IFileUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask) // <-- strict model doesn't allow to setup exceptions
                .Verifiable();

            mockStreamValidationService
                .Setup(s => s.ValidateStreamOrThrow(
                    It.IsAny<long>(),
                    It.IsAny<Stream>()))
                .Throws(new ValidationException(
                 new Dictionary<string, string[]>
                 {
                     ["OpenReadStream"] = [errorMessage]
                 }))
                .Verifiable();

            // Act
            var (results, message) = await photoService.UploadPhotoAsync(photos, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Is.Not.Null);
                Assert.That(results.Count(), Is.EqualTo(1));

                var result = results.Single();
                Assert.That(result, Is.Not.Null);
                Assert.That(message, Is.EqualTo(Messages.SuccesMessage.FailedToUploadAllPhotos));

                Assert.That(result.IsSuccess, Is.False);

                var startLog = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(startLog, "Count"), Is.EqualTo(photos.Count));

                var blobStartLog = logger.Entries.Single(e => e.EventId.Id == 3);
                Assert.That(blobStartLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobStartLog, "FileName"), Is.EqualTo(fileName));

                var validationLog = logger.Entries.Single(e => e.EventId.Id == 6);
                Assert.That(validationLog.Level, Is.EqualTo(LogLevel.Warning));
                Assert.That(TestLogger<PhotoService>.GetStateValue(validationLog, "FileName"), Is.EqualTo(fileName));

                var loggedMessages = (IReadOnlyCollection<string>)TestLogger<PhotoService>.GetStateValue(validationLog, "Messages")!;
                Assert.That(loggedMessages.Single(), Is.EqualTo(errorMessage));
            }

            mockValidationService.Verify(s => s.ValidateAndThrowAllAsync(mockPhoto.Object, It.IsAny<CancellationToken>()), Times.Once);
            mockStreamValidationService.Verify(s => s.ValidateStreamOrThrow(It.IsAny<long>(), It.IsAny<Stream>()), Times.Once);
            mockBlobStorageService.VerifyNoOtherCalls();
            mockQueueStorageService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenBlobUploadFails_ShouldReturnFailureForThatPhoto()
        {
            // Arrange
            string containerName = Messages.Constants.BlobContainerName;
            var photo = CreateValidPhoto(1);
            string blobName = $"{containerName}/{photo.FileName}";
            string trackingId = Guid.NewGuid().ToString("N");
            var photos = new List<IFileUploadRequest> { photo };

            var errorMessage = "Unexpected blob upload failure.";

            var blobFailureResult = BlobUploadResult.CreateFailure(errorMessage, ErrorCodes.BlobUploadFailed);

            mockValidationService
                .Setup(s => s.ValidateAndThrowAllAsync(
                    It.IsAny<IFileUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            mockStreamValidationService
                .Setup(s => s.ValidateStreamOrThrow(
                    It.IsAny<long>(),
                    It.IsAny<Stream>()))
                .Verifiable();

            BlobUploadRequest? capturedRequest = null;

            mockBlobStorageService
               .Setup(s => s.UploadFileAsync(
                   It.IsAny<BlobUploadRequest>(),
                   It.IsAny<CancellationToken>()))
               .Callback<BlobUploadRequest, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(blobFailureResult)
               .Verifiable();

            // Act
            var (results, message) = await photoService.UploadPhotoAsync(photos, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Is.Not.Null);
                Assert.That(results.Count(), Is.EqualTo(1));
                Assert.That(message, Is.EqualTo(Messages.SuccesMessage.FailedToUploadAllPhotos));

                var result = results.Single();
                Assert.That(result, Is.Not.Null);
                Assert.That(result.IsSuccess, Is.False);

                Assert.That(result.BlobResult, Is.Not.Null);
                Assert.That(result.BlobResult.IsSuccess, Is.False);
                Assert.That(result.BlobResult.ErrorMessage, Is.EqualTo(errorMessage));
                Assert.That(result.BlobResult.ErrorCode, Is.EqualTo(ErrorCodes.BlobUploadFailed));

                Assert.That(capturedRequest, Is.Not.Null);
                Assert.That(result.BlobName, Is.EqualTo(capturedRequest!.BlobName));
                Assert.That(result.TrackingId, Is.EqualTo(capturedRequest.TrackingId));
                Assert.That(capturedRequest.FileName, Is.EqualTo(photo.FileName));
                Assert.That(capturedRequest.ContentType, Is.EqualTo(photo.ContentType));
                Assert.That(capturedRequest.BlobName, Does.StartWith(Messages.Constants.BlobContainerName));

                var startLog = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(startLog, "Count"), Is.EqualTo(photos.Count));

                var blobStartLog = logger.Entries.Single(e => e.EventId.Id == 3);
                Assert.That(blobStartLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobStartLog, "FileName"), Is.EqualTo(photo.FileName));

                var blobFailedLog = logger.Entries.Single(e => e.EventId.Id == 5);
                Assert.That(blobFailedLog.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobFailedLog, "FileName"), Is.EqualTo(photo.FileName));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobFailedLog, "ErrorMessage"), Is.EqualTo(errorMessage));

                mockValidationService
                    .Verify(s => s.ValidateAndThrowAllAsync(
                        It.IsAny<IFileUploadRequest>(),
                        CancellationToken.None),
                        Times.Once);

                mockStreamValidationService
                    .Verify(s => s.ValidateStreamOrThrow(
                        It.IsAny<long>(),
                        It.IsAny<Stream>()), Times.Once);
                mockBlobStorageService
                    .Verify(s => s.UploadFileAsync(
                        It.IsAny<BlobUploadRequest>(),
                        CancellationToken.None), Times.Once);

                mockQueueStorageService.Verify(
                    s => s.EnqueueAsync(
                        It.IsAny<QueueMessageRequest>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenOneBlobFailAndOnePass_ThenQueuePass_MixResult_OK()
        {
            // Arrange
            var photo1 = CreateValidPhoto(1);
            var errorMessage = "Unexpected blob upload failure.";
            var blobFailureResult = BlobUploadResult.CreateFailure(errorMessage, ErrorCodes.BlobUploadFailed);

            var photo2 = CreateValidPhoto(2);
            var blobSuccessResult = BlobUploadResult.CreateSuccess("etag");
            var queueSuccessResult = QueuePushResult.CreateSuccess("1");

            var photos = new List<IFileUploadRequest> { photo1, photo2 };

            mockValidationService
                .Setup(s => s.ValidateAndThrowAllAsync(
                    It.IsAny<IFileUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            mockStreamValidationService
                .Setup(s => s.ValidateStreamOrThrow(
                    It.IsAny<long>(),
                    It.IsAny<Stream>()))
                .Verifiable();

            var capturedBlobRequests = new List<BlobUploadRequest>();
            var blobResults = new Queue<BlobUploadResult>([blobFailureResult, blobSuccessResult]);

            mockBlobStorageService
                .Setup(s => s.UploadFileAsync(
                    It.IsAny<BlobUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<BlobUploadRequest, CancellationToken>((req, _) => capturedBlobRequests.Add(req))
                .ReturnsAsync(() => blobResults.Dequeue())
                .Verifiable();

            QueueMessageRequest? capturedQueueRequest = null;
            mockQueueStorageService
               .Setup(s => s.EnqueueAsync(
                   It.IsAny<QueueMessageRequest>(),
                   It.IsAny<CancellationToken>()))
               .Callback<QueueMessageRequest, CancellationToken>((req, _) => capturedQueueRequest = req)
               .ReturnsAsync(queueSuccessResult)
               .Verifiable();

            // Act
            var (results, message) = await photoService.UploadPhotoAsync(photos, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                var startLog = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(startLog, "Count"), Is.EqualTo(photos.Count));

                var resultsList = results.ToList();
                Assert.That(resultsList, Has.Count.EqualTo(2));
                Assert.That(message, Is.EqualTo(Messages.SuccesMessage.SomePhotosUploadedSuccessfully));

                Assert.That(capturedBlobRequests, Has.Count.EqualTo(2));
                var blobReq1 = capturedBlobRequests.Single(r => r.FileName == photo1.FileName);
                var blobReq2 = capturedBlobRequests.Single(r => r.FileName == photo2.FileName);

                var resultFailBlob = resultsList.Single(r => r.FileName == photo1.FileName);
                Assert.That(resultFailBlob.BlobName, Is.EqualTo(blobReq1.BlobName));
                Assert.That(resultFailBlob.TrackingId, Is.EqualTo(blobReq1.TrackingId));
                Assert.That(resultFailBlob.BlobResult.IsSuccess, Is.False);
                Assert.That(resultFailBlob.BlobResult.ErrorMessage, Is.EqualTo(errorMessage));
                Assert.That(resultFailBlob.BlobResult.ErrorCode, Is.EqualTo(ErrorCodes.BlobUploadFailed));
                Assert.That(resultFailBlob.IsSuccess, Is.False);

                var resultSuccessBlob = resultsList.Single(r => r.FileName == photo2.FileName);
                Assert.That(resultSuccessBlob.BlobName, Is.EqualTo(blobReq2.BlobName));
                Assert.That(resultSuccessBlob.TrackingId, Is.EqualTo(blobReq2.TrackingId));
                Assert.That(resultSuccessBlob.BlobResult.IsSuccess, Is.True);
                Assert.That(resultSuccessBlob.QueueResult.IsSuccess, Is.True);
                Assert.That(resultSuccessBlob.IsSuccess, Is.True);

                Assert.That(capturedQueueRequest, Is.Not.Null);
                Assert.That(capturedQueueRequest!.BlobName, Is.EqualTo(resultSuccessBlob.BlobName));
                Assert.That(capturedQueueRequest.TrackingId, Is.EqualTo(resultSuccessBlob.TrackingId));

                var blobStartLogs = logger.Entries.Where(e => e.EventId.Id == 3).ToList();
                Assert.That(blobStartLogs, Has.Count.EqualTo(2));
                Assert.That(
                    blobStartLogs.Select(l => (string)TestLogger<PhotoService>.GetStateValue(l, "FileName")!).ToHashSet(),
                    Is.EquivalentTo([photo1.FileName, photo2.FileName]));

                var blobFailedLogs = logger.Entries.Where(e => e.EventId.Id == 5).ToList();
                Assert.That(blobFailedLogs, Has.Count.EqualTo(1));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobFailedLogs.Single(), "FileName"), Is.EqualTo(photo1.FileName));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobFailedLogs.Single(), "ErrorMessage"), Is.EqualTo(errorMessage));

                var blobSuccessLogs = logger.Entries.Where(e => e.EventId.Id == 4).ToList();
                Assert.That(blobSuccessLogs, Has.Count.EqualTo(1));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobSuccessLogs.Single(), "FileName"), Is.EqualTo(photo2.FileName));

                var queueSuccessLogs = logger.Entries.Where(e => e.EventId.Id == 7).ToList();
                Assert.That(queueSuccessLogs, Has.Count.EqualTo(1));
                Assert.That(TestLogger<PhotoService>.GetStateValue(queueSuccessLogs.Single(), "BlobName"), Is.EqualTo(resultSuccessBlob.BlobName));

                mockValidationService
                    .Verify(s => s.ValidateAndThrowAllAsync(
                        It.IsAny<IFileUploadRequest>(),
                        CancellationToken.None),
                        Times.Exactly(2));

                mockStreamValidationService
                    .Verify(s => s.ValidateStreamOrThrow(
                        It.IsAny<long>(),
                        It.IsAny<Stream>()), Times.Exactly(2));

                mockBlobStorageService
                    .Verify(s => s.UploadFileAsync(
                        It.IsAny<BlobUploadRequest>(),
                        CancellationToken.None), Times.Exactly(2));

                mockQueueStorageService.Verify(
                    s => s.EnqueueAsync(
                        It.IsAny<QueueMessageRequest>(),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenBlobUploadSucceeds_ButQueueFails_ShouldReturnFailureForThatPhoto()
        {
            // Arrange
            var photo = CreateValidPhoto(1);
            var blobSuccessResult = BlobUploadResult.CreateSuccess("etag");
            var queueFailureResult = QueuePushResult.CreateFailure("Queue service unavailable", ErrorCodes.QueuePushFailed);

            var photos = new List<IFileUploadRequest> { photo };

            mockValidationService
                .Setup(s => s.ValidateAndThrowAllAsync(
                    It.IsAny<IFileUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            mockStreamValidationService
                .Setup(s => s.ValidateStreamOrThrow(
                    It.IsAny<long>(),
                    It.IsAny<Stream>()))
                .Verifiable();

            mockBlobStorageService
                .Setup(s => s.UploadFileAsync(
                    It.IsAny<BlobUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(blobSuccessResult)
                .Verifiable();

            QueueMessageRequest? capturedQueueRequest = null;
            mockQueueStorageService
               .Setup(s => s.EnqueueAsync(
                   It.IsAny<QueueMessageRequest>(),
                   It.IsAny<CancellationToken>()))
               .Callback<QueueMessageRequest, CancellationToken>((req, _) => capturedQueueRequest = req)
               .ReturnsAsync(queueFailureResult)
               .Verifiable();

            // Act
            var (results, message) = await photoService.UploadPhotoAsync(photos, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Is.Not.Null);
                Assert.That(results.Count(), Is.EqualTo(1));
                Assert.That(message, Is.EqualTo(Messages.SuccesMessage.FailedToUploadAllPhotos));

                var result = results.Single();
                Assert.That(result, Is.Not.Null);
                Assert.That(result.BlobResult.IsSuccess, Is.True);
                Assert.That(result.QueueResult.IsSuccess, Is.False);
                Assert.That(result.QueueResult.ErrorMessage, Is.EqualTo(queueFailureResult.ErrorMessage));
                Assert.That(result.QueueResult.ErrorCode, Is.EqualTo(queueFailureResult.ErrorCode));
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(capturedQueueRequest, Is.Not.Null);
                Assert.That(capturedQueueRequest!.BlobName, Is.EqualTo(result.BlobName));
                Assert.That(capturedQueueRequest!.TrackingId, Is.EqualTo(result.TrackingId));

                var startLog = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(startLog, "Count"), Is.EqualTo(photos.Count));

                var blobStartLog = logger.Entries.Single(e => e.EventId.Id == 3);
                Assert.That(blobStartLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobStartLog, "FileName"), Is.EqualTo(photo.FileName));

                var blobSuccessLogs = logger.Entries.Single(e => e.EventId.Id == 4);
                Assert.That(blobStartLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobSuccessLogs, "FileName"), Is.EqualTo(photo.FileName));
                Assert.That(TestLogger<PhotoService>.GetStateValue(blobSuccessLogs, "BlobName"), Is.EqualTo(result.BlobName));

                var failedQueueLog = logger.Entries.Single(e => e.EventId.Id == 8);
                Assert.That(failedQueueLog.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<PhotoService>.GetStateValue(failedQueueLog, "BlobName"), Is.EqualTo(result.BlobName));
                Assert.That(TestLogger<PhotoService>.GetStateValue(failedQueueLog, "ErrorMessage"), Is.EqualTo(queueFailureResult.ErrorMessage));
                Assert.That(TestLogger<PhotoService>.GetStateValue(failedQueueLog, "ErrorCode"), Is.EqualTo(queueFailureResult.ErrorCode));

                mockValidationService
                    .Verify(s => s.ValidateAndThrowAllAsync(
                        It.IsAny<IFileUploadRequest>(),
                        CancellationToken.None),
                        Times.Once);

                mockStreamValidationService
                    .Verify(s => s.ValidateStreamOrThrow(
                        It.IsAny<long>(),
                        It.IsAny<Stream>()), Times.Once);

                mockBlobStorageService
                    .Verify(s => s.UploadFileAsync(
                        It.IsAny<BlobUploadRequest>(),
                        CancellationToken.None), Times.Once);

                mockQueueStorageService.Verify(
                    s => s.EnqueueAsync(
                        It.IsAny<QueueMessageRequest>(),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Test]
        public async Task PhotoService_UploadPhotoAsync_WhenTwoBlobSuccess_ThenOnlyOneQueuePass_MixResult_OK()
        {
            // Arrange
            var photo1 = CreateValidPhoto(1);
            var blobSuccessResult1 = BlobUploadResult.CreateSuccess("etag");
            var queueSuccessResult1 = QueuePushResult.CreateSuccess("1");

            var photo2 = CreateValidPhoto(2);
            var blobSuccessResult2 = BlobUploadResult.CreateSuccess("etag2");
            var queueErrorMessage = "Queue service unavailable";
            var queueFailureResult2 = QueuePushResult.CreateFailure(queueErrorMessage, ErrorCodes.QueuePushFailed);

            var photos = new List<IFileUploadRequest> { photo1, photo2 };

            mockValidationService
                .Setup(s => s.ValidateAndThrowAllAsync(
                    It.IsAny<IFileUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            mockStreamValidationService
                .Setup(s => s.ValidateStreamOrThrow(
                    It.IsAny<long>(),
                    It.IsAny<Stream>()))
                .Verifiable();

            var capturedBlobRequests = new List<BlobUploadRequest>();
            var blobResults = new Queue<BlobUploadResult>([blobSuccessResult1, blobSuccessResult2]);

            mockBlobStorageService
                .Setup(s => s.UploadFileAsync(
                    It.IsAny<BlobUploadRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<BlobUploadRequest, CancellationToken>((req, _) => capturedBlobRequests.Add(req))
                .ReturnsAsync(() => blobResults.Dequeue())
                .Verifiable();

            var capturedQueueRequests = new List<QueueMessageRequest>();
            var queueResults = new Queue<QueuePushResult>([queueSuccessResult1, queueFailureResult2]);

            mockQueueStorageService
               .Setup(s => s.EnqueueAsync(
                   It.IsAny<QueueMessageRequest>(),
                   It.IsAny<CancellationToken>()))
               .Callback<QueueMessageRequest, CancellationToken>((req, _) => capturedQueueRequests.Add(req))
               .ReturnsAsync(() => queueResults.Dequeue())
               .Verifiable();

            // Act
            var (results, message) = await photoService.UploadPhotoAsync(photos, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                var startLog = logger.Entries.Single(e => e.EventId.Id == 1);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(startLog, "Count"), Is.EqualTo(photos.Count));

                var resultsList = results.ToList();
                Assert.That(resultsList, Has.Count.EqualTo(2));
                Assert.That(message, Is.EqualTo(Messages.SuccesMessage.SomePhotosUploadedSuccessfully));

                Assert.That(capturedBlobRequests, Has.Count.EqualTo(2));
                var blobReq1 = capturedBlobRequests.Single(r => r.FileName == photo1.FileName);
                var blobReq2 = capturedBlobRequests.Single(r => r.FileName == photo2.FileName);

                var resultSuccessPhoto1 = resultsList.Single(r => r.FileName == photo1.FileName);
                Assert.That(resultSuccessPhoto1.BlobName, Is.EqualTo(blobReq1.BlobName));
                Assert.That(resultSuccessPhoto1.TrackingId, Is.EqualTo(blobReq1.TrackingId));
                Assert.That(resultSuccessPhoto1.BlobResult.IsSuccess, Is.True);
                Assert.That(resultSuccessPhoto1.IsSuccess, Is.True);
                Assert.That(resultSuccessPhoto1.QueueResult.IsSuccess, Is.True);

                var resultSuccessPhoto2 = resultsList.Single(r => r.FileName == photo2.FileName);
                Assert.That(resultSuccessPhoto2.BlobName, Is.EqualTo(blobReq2.BlobName));
                Assert.That(resultSuccessPhoto2.TrackingId, Is.EqualTo(blobReq2.TrackingId));
                Assert.That(resultSuccessPhoto2.BlobResult.IsSuccess, Is.True);
                Assert.That(resultSuccessPhoto2.IsSuccess, Is.False);
                Assert.That(resultSuccessPhoto2.QueueResult.IsSuccess, Is.False);

                Assert.That(capturedQueueRequests, Is.Not.Null);
                Assert.That(capturedQueueRequests, Has.Count.EqualTo(2));
                var queueReq1 = capturedQueueRequests.Single(r => r.BlobName == blobReq1.BlobName);
                var queueReq2 = capturedQueueRequests.Single(r => r.BlobName == blobReq2.BlobName);

                var resultSuccessQueue1 = resultsList.Single(r => r.FileName == blobReq1.FileName);
                Assert.That(resultSuccessQueue1.QueueResult.IsSuccess, Is.True);
                Assert.That(resultSuccessQueue1.IsSuccess, Is.True);
                Assert.That(resultSuccessQueue1.TrackingId, Is.EqualTo(queueReq1.TrackingId));
                Assert.That(resultSuccessQueue1.BlobName, Is.EqualTo(queueReq1.BlobName));

                var resultFailQueue2 = resultsList.Single(r => r.FileName == blobReq2.FileName);
                Assert.That(resultFailQueue2.QueueResult.IsSuccess, Is.False);
                Assert.That(resultFailQueue2.IsSuccess, Is.False);
                Assert.That(resultFailQueue2.BlobName, Is.EqualTo(queueReq2.BlobName));
                Assert.That(resultFailQueue2.QueueResult.ErrorCode, Is.EqualTo(ErrorCodes.QueuePushFailed));
                Assert.That(resultFailQueue2.QueueResult.ErrorMessage, Is.EqualTo(queueErrorMessage));

                var blobStartLogs = logger.Entries.Where(e => e.EventId.Id == 3).ToList();
                Assert.That(blobStartLogs, Has.Count.EqualTo(2));
                Assert.That(
                    blobStartLogs.Select(l => (string)TestLogger<PhotoService>.GetStateValue(l, "FileName")!).ToHashSet(),
                    Is.EquivalentTo([photo1.FileName, photo2.FileName]));

                var blobSuccessLogs = logger.Entries.Where(e => e.EventId.Id == 4).ToList();
                Assert.That(blobSuccessLogs, Has.Count.EqualTo(2));
                Assert.That(
                    blobSuccessLogs.Select(l => (string)TestLogger<PhotoService>.GetStateValue(l, "FileName")!).ToHashSet(),
                    Is.EquivalentTo([photo1.FileName, photo2.FileName]));

                var queueSuccessLog = logger.Entries.Single(e => e.EventId.Id == 7);
                Assert.That(queueSuccessLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(queueSuccessLog, "BlobName"), Is.EqualTo(resultSuccessPhoto1.BlobName));

                var queueErrorLog = logger.Entries.Single(e => e.EventId.Id == 8);
                Assert.That(queueErrorLog.Level, Is.EqualTo(LogLevel.Error));
                Assert.That(TestLogger<PhotoService>.GetStateValue(queueErrorLog, "BlobName"), Is.EqualTo(queueReq2.BlobName));
                Assert.That(TestLogger<PhotoService>.GetStateValue(queueErrorLog, "ErrorMessage"), Is.EqualTo(queueFailureResult2.ErrorMessage));
                Assert.That(TestLogger<PhotoService>.GetStateValue(queueErrorLog, "ErrorCode"), Is.EqualTo(queueFailureResult2.ErrorCode));

                mockValidationService
                    .Verify(s => s.ValidateAndThrowAllAsync(
                        It.IsAny<IFileUploadRequest>(),
                        CancellationToken.None),
                        Times.Exactly(2));

                mockStreamValidationService
                    .Verify(s => s.ValidateStreamOrThrow(
                        It.IsAny<long>(),
                        It.IsAny<Stream>()), Times.Exactly(2));

                mockBlobStorageService
                    .Verify(s => s.UploadFileAsync(
                        It.IsAny<BlobUploadRequest>(),
                        CancellationToken.None), Times.Exactly(2));

                mockQueueStorageService.Verify(
                    s => s.EnqueueAsync(
                        It.IsAny<QueueMessageRequest>(),
                        It.IsAny<CancellationToken>()),
                    Times.Exactly(2));
            }
        }

        #endregion

        #region DownloadPhotoAsync

        [Test]
        public async Task PhotoService_DownloadPhotoAsync_ShouldMapBlobDownloadData_Ok()
        {
            // Arrange
            const string blobName = "photos/abc.png";
            const string contentType = "image/png";
            const string fileName = "abc.png";
            var ct = CancellationToken.None;

            var content = new MemoryStream("hello"u8.ToArray());
            var blob = new BlobDownloadData
            {
                Content = content,
                ContentType = contentType,
                FileName = fileName
            };

            mockBlobStorageService
                .Setup(s => s.DownloadBlobAsync(blobName, ct))
                .ReturnsAsync(blob)
                .Verifiable();

            // Act
            var result = await photoService.DownloadPhotoAsync(blobName, ct);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Content, Is.SameAs(content));
                Assert.That(result.ContentType, Is.EqualTo(contentType));
                Assert.That(result.FileName, Is.EqualTo(fileName));

                var startLog = logger.Entries.Single(e => e.EventId.Id == 9);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(startLog, "BlobName"), Is.EqualTo(blobName));

                var successLog = logger.Entries.Single(e => e.EventId.Id == 10);
                Assert.That(successLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(successLog, "BlobName"), Is.EqualTo(blobName));
            }

            mockBlobStorageService.Verify(s => s.DownloadBlobAsync(blobName, ct), Times.Once);
        }

        [Test]
        public async Task PhotoService_DownloadPhotoAsync_WhenBlobFileNameIsNull_ShouldFallbackToBlobName()
        {
            // Arrange
            const string blobName = "photos/no-metadata.png";
            const string contentType = "image/png";

            var content = new MemoryStream("hello"u8.ToArray());
            var blob = new BlobDownloadData
            {
                Content = content,
                ContentType = contentType,
                FileName = null // <--
            };

            mockBlobStorageService
                .Setup(s => s.DownloadBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(blob)
                .Verifiable();

            // Act
            var result = await photoService.DownloadPhotoAsync(blobName, CancellationToken.None);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FileName, Is.EqualTo(blobName));
                Assert.That(result.Content, Is.SameAs(content));
                Assert.That(result.ContentType, Is.EqualTo(contentType));
            }

            mockBlobStorageService.Verify(s => s.DownloadBlobAsync(blobName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void PhotoService_DownloadPhotoAsync_WhenBlobStorageThrows_ShouldBubbleUp_AndNotLogSuccess()
        {
            // Arrange
            const string blobName = "photos/missing.png";

            mockBlobStorageService
                .Setup(s => s.DownloadBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("storage failure"))
                .Verifiable();

            // Act & Assert
            _ = Assert.ThrowsAsync<InvalidOperationException>(()
                => photoService.DownloadPhotoAsync(blobName, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logger.ContainsEventId(9), Is.True, "Start download log should be present.");
                Assert.That(logger.ContainsEventId(10), Is.False, "Success download log should not be present when failing.");
            }

            mockBlobStorageService
                .Verify(s => s.DownloadBlobAsync(blobName, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetUploadedPhotosInfosAsync

        [Test]
        public async Task PhotoService_GetUploadedPhotosInfosAsync_ShouldReturnEmptyList()
        {
            // Arrange
            mockBlobStorageService
                .Setup(s => s.GetBlobsInfoAsync())
                .ReturnsAsync([])
                .Verifiable();

            // Act
            var result = await photoService.GetUploadedPhotosInfosAsync();

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Empty);
                Assert.That(result, Is.Not.Null);

                var startLog = logger.Entries.Single(e => e.EventId.Id == 11);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Debug));

                var successLog = logger.Entries.Single(e => e.EventId.Id == 12);
                Assert.That(successLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(successLog, "BlobsCount"), Is.EqualTo(0));

                mockBlobStorageService.Verify(s => s.GetBlobsInfoAsync(), Times.Once);
            }
        }

        [Test]
        public async Task PhotoService_GetUploadedPhotosInfosAsync_ShouldReturnBlobData()
        {
            // Arrange
            var blobData1 = new BlobData("5646sfd/photos1/jpg", "photo1.jpg", DateTime.UtcNow.AddDays(-1));
            var blobData2 = new BlobData("q4ds45/photos2/jpg", "photo2.jpg", DateTime.UtcNow.AddDays(-2));
            var blobDataList = new List<BlobData> { blobData1, blobData2 };

            mockBlobStorageService
                .Setup(s => s.GetBlobsInfoAsync())
                .ReturnsAsync(blobDataList)
                .Verifiable();

            // Act
            var result = await photoService.GetUploadedPhotosInfosAsync();

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.SameAs(blobDataList));
                Assert.That(result.Count(), Is.EqualTo(2));

                var startLog = logger.Entries.Single(e => e.EventId.Id == 11);
                Assert.That(startLog.Level, Is.EqualTo(LogLevel.Debug));

                var successLog = logger.Entries.Single(e => e.EventId.Id == 12);
                Assert.That(successLog.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(TestLogger<PhotoService>.GetStateValue(successLog, "BlobsCount"), Is.EqualTo(2));

                mockBlobStorageService.Verify(s => s.GetBlobsInfoAsync(), Times.Once);
            }
        }

        #endregion
    }
}
