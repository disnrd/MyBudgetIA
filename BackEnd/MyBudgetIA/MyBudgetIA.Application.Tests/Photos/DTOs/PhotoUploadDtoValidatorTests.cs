using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Application.Validators.Photo;
using FluentValidation;
using FluentValidation.TestHelper;
using Messages = MyBudgetIA.Application.Validators.Photo.PhotoUploadDtoValidator.Messages;
using MyBudgetIA.Domain.Constraints;

namespace MyBudgetIA.Application.Tests.Photos.DTOs
{
    /// <summary>
    /// Unit tests for <see cref="PhotoUploadDtoValidator"/>.
    /// </summary>
    [TestFixture]
    public class PhotoUploadDtoValidatorTests
    {
        private PhotoUploadDtoValidator validator;

        #region SetUp

        [SetUp]
        public void SetUp()
        {
            validator = new PhotoUploadDtoValidator();
        }

        #endregion

        #region Constructor

        [Test]
        public void PhotoUploadDtoValidator_Constructor_Ok()
        {
            Assert.That(
                () => new PhotoUploadDtoValidator(),
                Throws.Nothing);
        }

        #endregion

        #region Valid

        [Test]
        public async Task PhotoUploadDtoValidator_Valid_Ok()
        {
            // Arrange
            var dto = new PhotoUploadDto
            (
                FileName: "valid_photo.jpg",
                ContentType: "image/jpg",
                Length: 1024,
                Content: new MemoryStream(new byte[1024]),
                Extension: ".jpg"
            );

            // Act
            var result = await validator.ValidateAsync(dto);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region CreateValidDto

        /// As PhotoUploadDto is not an Api input but a manually created object, his properties are not nullable.
        /// Therefore, to avoid creating value for each property in every test, we create a valid instance and modify
        /// what is necessary for each test.
        private static PhotoUploadDto CreateValidDto() => new(
            FileName: "valid_photo.jpg",
            ContentType: "image/jpeg",
            Length: 1024,
            Content: new MemoryStream(new byte[1024]),
            Extension: ".jpg");

        private static PhotoUploadDto With(
            PhotoUploadDto source,
            string? fileName = null,
            string? contentType = null,
            long? length = null,
            Stream? content = null,
            string? extension = null)
            => source with
            {
                FileName = fileName ?? source.FileName,
                ContentType = contentType ?? source.ContentType,
                Length = length ?? source.Length,
                Content = content ?? source.Content,
                Extension = extension ?? source.Extension
            };

        #endregion

        #region FileName

        [TestCase("", false)]
        [TestCase(" ", false)]
        [TestCase("fuz.jpg", true)]
        public async Task PhotoUploadDtoValidator_FileName_ShouldNotBeEmpty(
            string fileName,
            bool isValid)
        {
            // Arrange
            var dto = With(CreateValidDto(), fileName: fileName);

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
                    .Replace("{PropertyName}", nameof(PhotoUploadDto.FileName));
                result
                    .ShouldHaveValidationErrorFor(d => d.FileName)
                    .WithErrorMessage(expectedMessage);
            }
        }

        [TestCase("image1", false)]
        [TestCase("image2_bar ", false)]
        [TestCase("fuz.jpg", true)]
        public async Task PhotoUploadDtoValidator_FileName_ShouldHaveValidExtension(
           string fileName,
           bool isValid)
        {
            // Arrange
            var dto = With(CreateValidDto(), fileName: fileName);

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
        public async Task PhotoUploadDtoValidator_FileName_ShouldHaveValidForBlogStorage(
          string fileName,
          bool isValid)
        {
            // Arrange
            var dto = With(CreateValidDto(), fileName: fileName);

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
        public async Task PhotoUploadDtoValidator_FileNameExceedsMaxLength_ShouldHaveValidationError()
        {
            // Arrange
            var longFileName = new string('a', PhotoConstraints.MaxFileNameLength + 1) + ".jpg";
            var dto = With(CreateValidDto(), fileName: longFileName);

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

        [TestCase("")]
        [TestCase(" ")]
        public async Task PhotoUploadDtoValidator_ContentType_ShouldNotBeEmpty(
            string contentType)
        {
            // Arrange
            var dto = With(CreateValidDto(), contentType: contentType);

            // Act
            var result = await validator.TestValidateAsync(dto);

            var expectedMessage = Messages.MustNotBeEmptyProperty
                .Replace("{PropertyName}", nameof(PhotoUploadDto.ContentType));
            result
                .ShouldHaveValidationErrorFor(d => d.ContentType)
                .WithErrorMessage(expectedMessage);
        }

        [TestCase(".jpg", "image/png", false)]
        [TestCase(".png", "image/jpeg", false)]
        [TestCase(".heic", "image/heic", true)]
        public async Task PhotoUploadDtoValidator_ContentType_ShouldContentTypeMatchExtension(
            string extension,
            string contentType,
            bool isValid)
        {
            // Arrange
            var dto = With(CreateValidDto(), extension: extension, contentType: contentType);

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
        public async Task PhotoUploadDtoValidator_ContentType_ShouldBeAnAllowedContentType(
            string contentType,
            bool isValid)
        {
            // Arrange
            var dto = With(CreateValidDto(),
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
                    .WithErrorMessage(Messages.GetMustHaveValidContentTypeMessage());
            }
        }

        #endregion

        #region Length

        [Test]
        public async Task PhotoUploadDtoValidator_Length_ShouldBeGreaterThanZero()
        {
            // Arrange
            var dto = With(CreateValidDto(), length: 0);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            result
                .ShouldHaveValidationErrorFor(d => d.Length)
                .WithErrorMessage(Messages.MustBeGreaterThan
                    .Replace("{PropertyName}", nameof(PhotoUploadDto.Length)));
        }

        [Test]
        public async Task PhotoUploadDtoValidator_Length_ShouldBeLessOrEqualThanMaxLenght()
        {
            // Arrange
            var dto = With(CreateValidDto(), length: PhotoConstraints.MaxSizeInBytes + 1);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            result
                .ShouldHaveValidationErrorFor(d => d.Length)
                .WithErrorMessage(Messages.FileSizeCannotExceedMax);
        }

        [Test]
        public async Task PhotoUploadDtoValidator_Length_Valid()
        {
            // Arrange
            var dto = With(CreateValidDto(), length: PhotoConstraints.MaxSizeInBytes );

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(d => d.Length);
        }

        #endregion

        #region Content

        /// The folowing method is not used but kept for reference in case we need to set Content to null in tests.
        private static PhotoUploadDto WithNullContent(PhotoUploadDto source)
            => source with { Content = null! };

        private sealed class NonReadableStream : MemoryStream
        {
            public override bool CanRead => false;
        }

        private sealed class NonSeekableReadableStream : MemoryStream
        {
            public override bool CanSeek => false;
        }

        [Test]
        public async Task PhotoUploadDtoValidator_Content_ShouldNotBeNull()
        {
            // Arrange
            var dto = WithNullContent(CreateValidDto());

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            var expectedMessage = Messages.MustNotBeNullProperty
                .Replace("{PropertyName}", nameof(PhotoUploadDto.Content));
            result
                .ShouldHaveValidationErrorFor(d => d.Content)
                .WithErrorMessage(expectedMessage);
        }

        [Test]
        public async Task PhotoUploadDtoValidator_Content_ShouldBeReadable()
        {
            var dto = With(CreateValidDto(), content: new NonReadableStream());

            var result = await validator.TestValidateAsync(dto);

            result.ShouldHaveValidationErrorFor(d => d.Content)
                .WithErrorMessage(Messages.MustStreamBeReadable
                .Replace("{PropertyName}", nameof(PhotoUploadDto.Content)));
        }

        [Test]
        public async Task PhotoUploadDtoValidator_Content_ShouldBeSeekable()
        {
            var dto = With(CreateValidDto(), content: new NonSeekableReadableStream());

            var result = await validator.TestValidateAsync(dto);

            result.ShouldHaveValidationErrorFor(d => d.Content)
                .WithErrorMessage(Messages.MustStreamBeSeekable
                .Replace("{PropertyName}", nameof(PhotoUploadDto.Content)));
        }

        [Test]
        public async Task PhotoUploadDtoValidator_Content_ShouldMatchLength()
        {
            var ms = new MemoryStream(new byte[10]);

            var dto = With(CreateValidDto(), content: ms, length: 9); // mismatch

            var result = await validator.TestValidateAsync(dto);

            result.ShouldHaveValidationErrorFor(d => d.Content)
                  .WithErrorMessage(Messages.MustHaveSameContentAndFileLength);
        }

        [Test]
        public async Task PhotoUploadDtoValidator_Content_Valid()
        {
            // Arrange
            var ms = new MemoryStream(new byte[10])
            {
                Position = 0
            };

            var dto = With(CreateValidDto(),
                content: ms,
                length: ms.Length);

            // Act
            var result = await validator.TestValidateAsync(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(d => d.Content);
        }

        #endregion

        #region Extension

        [TestCase("")]
        [TestCase(" ")]
        public async Task PhotoUploadDtoValidator_Extension_ShouldNotBeEmpty(
            string extension)
        {
            // Arrange
            var dto = With(CreateValidDto(), extension: extension);

            // Act
            var result = await validator.TestValidateAsync(dto);

            var expectedMessage = Messages.MustNotBeEmptyProperty
                .Replace("{PropertyName}", nameof(PhotoUploadDto.Extension));
            result
                .ShouldHaveValidationErrorFor(d => d.Extension)
                .WithErrorMessage(expectedMessage);
        }

        [TestCase(".zip", false)]
        [TestCase(".pdf", false)]
        [TestCase(".html", false)]
        [TestCase(".png", true)]
        public async Task PhotoUploadDtoValidator_Extension_ShouldBeAnAllowedExtension(
            string extension,
            bool isValid)
        {
            // Arrange
            var dto = With(CreateValidDto(), extension: extension);

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
                    .WithErrorMessage(Messages.GetMustHaveValidExtensionMessage());
            }
        }

        #endregion
    }
}
