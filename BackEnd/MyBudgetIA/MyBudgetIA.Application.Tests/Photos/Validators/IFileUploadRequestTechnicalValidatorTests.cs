using FluentValidation.TestHelper;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Application.Validators.Photo;
using Messages = MyBudgetIA.Application.Validators.Photo.IFileUploadRequestTechnicalValidator.Messages;


namespace MyBudgetIA.Application.Tests.Photos.Validators
{
    /// <summary>
    /// Unit tests for <see cref="IFileUploadRequestTechnicalValidator"/>.
    /// </summary>
    [TestFixture]
    public class IFileUploadRequestTechnicalValidatorTests
    {
        private IFileUploadRequestTechnicalValidator validator;

        #region SetUp

        [SetUp]
        public void SetUp()
        {
            validator = new IFileUploadRequestTechnicalValidator();
        }

        private sealed class TestFileUploadRequest(
            string fileName,
            string contentType,
            long length,
            string extension,
            Func<Stream>? openReadStream = null) : IFileUploadRequest
        {
            private readonly Func<Stream> _open = openReadStream ?? (() => new MemoryStream(new byte[length]));

            public string FileName { get; } = fileName;
            public string ContentType { get; } = contentType;
            public long Length { get; } = length;
            public string Extension { get; } = extension;

            public Stream OpenReadStream() => _open();
        }

        #endregion

        #region Constructor

        [Test]
        public void IFileUploadRequestTechnicalValidator_Constructor_Ok()
        {
            Assert.That(
                () => new IFileUploadRequestTechnicalValidator(),
                Throws.Nothing);
        }

        #endregion

        #region Valid

        [Test]
        public async Task IFileUploadRequestTechnicalValidator_Valid_Ok()
        {
            // Arrange
            var dto = new TestFileUploadRequest(
                fileName: "valid_photo.jpg",
                contentType: "image/jpeg",
                length: 1024,
                extension: ".jpg");

            // Act
            var result = await validator.ValidateAsync(dto);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region CreateValidRequest

        private static TestFileUploadRequest CreateValidRequest() =>
            new(
                fileName: "valid_photo.jpg",
                contentType: "image/jpeg",
                length: 1024,
                extension: ".jpg");

        private static TestFileUploadRequest With(
            IFileUploadRequest source,
            string? fileName = null,
            string? contentType = null,
            long? length = null,
            string? extension = null,
            Func<Stream>? openReadStream = null)
        {
            return new TestFileUploadRequest(
                fileName: fileName ?? source.FileName,
                contentType: contentType ?? source.ContentType,
                length: length ?? source.Length,
                extension: extension ?? source.Extension,
                openReadStream: openReadStream ?? source.OpenReadStream);
        }

        #endregion

        #region FileName

        [TestCase("", false)]
        [TestCase(" ", false)]
        [TestCase("fuz.jpg", true)]
        public async Task IFileUploadRequestTechnicalValidator_FileNameEmpty_ShouldNotBeEmpty(
            string fileName,
            bool isValid)
        {
            // Arrange
            var dto = With(CreateValidRequest(), fileName: fileName);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            if (isValid)
            {
                result.ShouldNotHaveValidationErrorFor(d => d.FileName);
            }
            else
            {
                var expectedMessage = Messages.MustNotBeEmptyProperty
                    .Replace("{PropertyName}", nameof(IFileUploadRequest.FileName));
                result
                    .ShouldHaveValidationErrorFor(d => d.FileName)
                    .WithErrorMessage(expectedMessage);
            }
        }

        [TestCase("image1", false)]
        [TestCase("image2_bar ", false)]
        [TestCase("fuz.jpg", true)]
        public async Task IFileUploadRequestTechnicalValidator_FileName_ShouldHaveValidExtension(
           string fileName,
           bool isValid)
        {
            // Arrange
            var dto = With(CreateValidRequest(), fileName: fileName);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            if (isValid)
            {
                result.ShouldNotHaveValidationErrorFor(d => d.FileName);
            }
            else
            {
                result
                    .ShouldHaveValidationErrorFor(d => d.FileName)
                    .WithErrorMessage(Messages.MustFileNameHaveExtension);
            }
        }

        #endregion

        #region ContentType

        [TestCase("")]
        [TestCase(" ")]
        public async Task IFileUploadRequestTechnicalValidator_ContentType_ShouldNotBeEmpty(
            string contentType)
        {
            // Arrange
            var dto = With(CreateValidRequest(), contentType: contentType);

            // Act
            var result = await validator.TestValidateAsync(dto);

            var expectedMessage = Messages.MustNotBeEmptyProperty
                .Replace("{PropertyName}", nameof(IFileUploadRequest.ContentType));
            result
                .ShouldHaveValidationErrorFor(d => d.ContentType)
                .WithErrorMessage(expectedMessage);
        }

        #endregion

        #region Length

        [Test]
        public async Task IFileUploadRequestTechnicalValidator_Length_ShouldBeGreaterThanZero()
        {
            // Arrange
            var dto = With(CreateValidRequest(), length: 0);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            result
                .ShouldHaveValidationErrorFor(d => d.Length)
                .WithErrorMessage(Messages.MustBeGreaterThan
                    .Replace("{PropertyName}", nameof(IFileUploadRequest.Length)));
        }

        #endregion

        #region Extension

        [TestCase("")]
        [TestCase(" ")]
        public async Task IFileUploadRequestTechnicalValidator_Extension_ShouldNotBeEmpty(
            string extension)
        {
            // Arrange
            var dto = With(CreateValidRequest(), extension: extension);

            // Act
            var result = await validator.TestValidateAsync(dto);

            var expectedMessage = Messages.MustNotBeEmptyProperty
                .Replace("{PropertyName}", nameof(IFileUploadRequest.Extension));
            result
                .ShouldHaveValidationErrorFor(d => d.Extension)
                .WithErrorMessage(expectedMessage);
        }

        #endregion
    }
}
