using FluentValidation.TestHelper;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Application.Validators.Photo;
using MyBudgetIA.Domain.Constraints;
using Messages = MyBudgetIA.Application.Validators.Photo.IFileUploadRequestBusinessValidator.Messages;


namespace MyBudgetIA.Application.Tests.Photos.Validators
{
    /// <summary>
    /// Unit tests for <see cref="IFileUploadRequestBusinessValidator"/>.
    /// </summary>
    [TestFixture]
    public class IFileUploadRequestBusinessValidatorTests
    {
        private IFileUploadRequestBusinessValidator validator;

        #region SetUp

        [SetUp]
        public void SetUp()
        {
            validator = new IFileUploadRequestBusinessValidator();
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
        public void IFileUploadRequestBusinessValidator_Constructor_Ok()
        {
            Assert.That(
                () => new IFileUploadRequestBusinessValidator(),
                Throws.Nothing);
        }

        #endregion

        #region Valid

        [Test]
        public async Task IFileUploadRequestBusinessValidator_Valid_Ok()
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

        [TestCase(".", false)]
        [TestCase("..", false)]
        [TestCase("image1*2", false)]
        [TestCase("img:1", false)]
        [TestCase("img\"A", false)]
        [TestCase("img/1", false)]
        [TestCase("img>1", false)]
        [TestCase("img?1", false)]
        [TestCase("img\u007f'1", false)]
        [TestCase("fuz.jpg", true)]
        public async Task IFileUploadRequestBusinessValidator_FileName_ShouldHaveValidForBlogStorage(
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
                    .WithErrorMessage(Messages.MustFileNameCharactersValidInBlobContext);
            }
        }

        [Test]
        public async Task IFileUploadRequestBusinessValidator_FileNameExceedsMaxLength_ShouldHaveValidationError()
        {
            // Arrange
            var longFileName = new string('a', PhotoConstraints.MaxFileNameLength + 1) + ".jpg";
            var dto = With(CreateValidRequest(), fileName: longFileName);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            result
                .ShouldHaveValidationErrorFor(d => d.FileName)
                .WithErrorMessage(Messages.FileNameCannotExceedMaxLength
                    .Replace("{MaxLength}", PhotoConstraints.MaxFileNameLength.ToString()));
        }

        #endregion

        #region ContentType

        [TestCase(".jpg", "image/png", false)]
        [TestCase(".png", "image/jpeg", false)]
        [TestCase(".heic", "image/heic", true)]
        public async Task IFileUploadRequestBusinessValidator_ContentType_ShouldContentTypeMatchExtension(
            string extension,
            string contentType,
            bool isValid)
        {
            // Arrange
            var dto = With(CreateValidRequest(), extension: extension, contentType: contentType);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            if (isValid)
            {
                result.ShouldNotHaveValidationErrorFor(d => d.ContentType);
            }
            else
            {
                result
                    .ShouldHaveValidationErrorFor(d => d.ContentType)
                    .WithErrorMessage(Messages.MustHaveSameContentTypeAsExtension);
            }
        }

        [TestCase("image/wrong", false)]
        [TestCase("image/nop", false)]
        [TestCase("image/heic", true)]
        public async Task IFileUploadRequestBusinessValidator_ContentType_ShouldBeAnAllowedContentType(
            string contentType,
            bool isValid)
        {
            // Arrange
            var dto = With(CreateValidRequest(),
                contentType: contentType, // <-- the one being tested
                extension: ".heic"); // to avoid conflict with extension-extension matching

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            if (isValid)
            {
                result.ShouldNotHaveValidationErrorFor(d => d.ContentType);
            }
            else
            {
                result
                    .ShouldHaveValidationErrorFor(d => d.ContentType)
                    .WithErrorMessage(Messages.GetMustHaveValidContentTypeMessage(contentType));
            }
        }

        #endregion


        #region Length

        [Test]
        public async Task IFileUploadRequestBusinessValidator_Length_ShouldBeLessOrEqualThanMaxLenght()
        {
            // Arrange
            var dto = With(CreateValidRequest(), length: PhotoConstraints.MaxSizeInBytes + 1);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            result
                .ShouldHaveValidationErrorFor(d => d.Length)
                .WithErrorMessage(Messages.FileSizeCannotExceedMax);
        }

        [Test]
        public async Task IFileUploadRequestBusinessValidator_Length_Valid()
        {
            // Arrange
            var dto = With(CreateValidRequest(), length: PhotoConstraints.MaxSizeInBytes);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(d => d.Length);
        }

        #endregion

        #region Extension

        [TestCase(".zip", false)]
        [TestCase(".pdf", false)]
        [TestCase(".html", false)]
        [TestCase(".png", true)]
        public async Task IFileUploadRequestBusinessValidator_Extension_ShouldBeAnAllowedExtension(
            string extension,
            bool isValid)
        {
            // Arrange
            var dto = With(CreateValidRequest(), extension: extension);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            if (isValid)
            {
                result.ShouldNotHaveValidationErrorFor(d => d.Extension);
            }
            else
            {
                result
                    .ShouldHaveValidationErrorFor(d => d.Extension)
                    .WithErrorMessage(Messages.GetMustHaveValidExtensionMessage(extension));
            }
        }

        #endregion
    }
}
