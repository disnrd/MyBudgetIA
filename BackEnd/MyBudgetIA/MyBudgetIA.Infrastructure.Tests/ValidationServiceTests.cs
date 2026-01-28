using FluentValidation;
using FluentValidation.Results;
using Moq;
using MyBudgetIA.Infrastructure.Services;

namespace MyBudgetIA.Infrastructure.Tests
{

    /// <summary>
    /// Unit tests for class <see cref="ValidationService"/>
    /// </summary>
    [TestFixture]
    public class ValidationServiceTests
    {
        private ValidationService _service = null!;
        private Mock<IServiceProvider> _provider = null!;
        private Mock<IValidator<TestDto>> _validator = null!;

        [SetUp]
        public void Setup()
        {
            _provider = new Mock<IServiceProvider>();
            _validator = new Mock<IValidator<TestDto>>();

            _provider
                .Setup(x => x.GetService(typeof(IValidator<TestDto>)))
                .Returns(_validator.Object);

            _service = new ValidationService(_provider.Object);
        }

        #region ValidateAndThrowAsync Tests

        [Test]
        public void ValidationService_ValidateAndThrowAsync_NoValidator_ShouldNotThrow()
        {
            // Arrange
            _provider
                .Setup(x => x.GetService(typeof(IValidator<TestDto>)))
                .Returns(null!)
                .Verifiable();

            // Act & Assert
            Assert.DoesNotThrowAsync(() => _service.ValidateAndThrowAsync(new TestDto()));
            _provider.Verify();
        }

        [Test]
        public void ValidationService_ValidateAndThrowAsync_Valid_ShouldNotThrow()
        {
            // Arrange
            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<TestDto>(), default))
                .ReturnsAsync(new ValidationResult())
                .Verifiable();

            // Act & Assert
            Assert.DoesNotThrowAsync(() => _service.ValidateAndThrowAsync(new TestDto()));
            _validator.Verify();
        }

        [Test]
        public void ValidationService_ValidateAndThrowAsync_Invalid_ShouldThrowValidationException()
        {
            // Arrange
            var failures = new List<ValidationFailure>
            {
                new("Name", "Name is required")
            };

            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<TestDto>(), default))
                .ReturnsAsync(new ValidationResult(failures))
                .Verifiable();

            // Act
            var ex =
                Assert.ThrowsAsync<Application.Exceptions.ValidationException>(
                    () => _service.ValidateAndThrowAsync(new TestDto()));

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.Errors.ContainsKey("Name"));
                Assert.That(ex.Errors["Name"][0], Is.EqualTo("Name is required"));
            }
            _validator.Verify();
        }

        [Test]
        public async Task ValidationService_ValidateAndThrowAsync_WithMultipleErrors_ShouldThrowWithAllErrors()
        {
            // Arrange
            var dto = new TestDto();

            var failures = new List<ValidationFailure>
            {
                new("Name", "Name is required"),
                new("Email", "Email is required"),
                new("Name", "Name must be at least 3 chars") // <-- same property, several errors
            };

            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<TestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures))
                .Verifiable();

            // Act & Assert
            var ex = Assert.ThrowsAsync<Application.Exceptions.ValidationException>(async () =>
                await _service.ValidateAndThrowAsync(dto));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Errors, Has.Count.EqualTo(2));
                Assert.That(ex.Errors["Name"], Has.Length.EqualTo(2));
            }
            _validator.Verify();
        }

        [Test]
        public async Task ValidationService_ValidateAndThrowAsync_ShouldCallValidatorOnce()
        {
            // Arrange
            var dto = new TestDto { Name = "Valid", Email = "valid@test.com" };

            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<TestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult())
                .Verifiable();

            // Act
            await _service.ValidateAndThrowAsync(dto);

            // Assert
            _validator.Verify(
                v => v.ValidateAsync(It.IsAny<TestDto>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region TryValidateAsync Tests

        [Test]
        public async Task ValidationService_TryValidateAsync_Valid_ShouldReturnTrueAndEmptyErrors()
        {
            // Arrange
            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<TestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult())
                .Verifiable();

            // Act
            var (isValid, errors) = await _service.TryValidateAsync(new TestDto());

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(isValid, Is.True);
                Assert.That(errors, Is.Empty);
            }
            _validator.Verify();
        }

        [Test]
        public async Task ValidationService_TryValidateAsync_Invalid_ShouldReturnFalseAndErrors()
        {
            // Arrange
            var failures = new List<ValidationFailure>
            {
                new("Age", "Age must be >= 18")
            };

            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<TestDto>(), default))
                .ReturnsAsync(new ValidationResult(failures))
                .Verifiable();

            // Act
            var (isValid, errors) = await _service.TryValidateAsync(new TestDto());

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.False);
                Assert.That(errors.ContainsKey("Age"));
            }
            _validator.Verify();
        }

        [Test]
        public async Task ValidationService_TryValidateAsync_WithNoValidator_ShouldReturnTrueAndEmptyErrors()
        {
            // Arrange
            _provider
                .Setup(x => x.GetService(typeof(IValidator<TestDto>)))
                .Returns(null!)
                .Verifiable();

            var service = new ValidationService(_provider.Object);

            // Act
            var (isValid, errors) = await service.TryValidateAsync(new TestDto());

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.True);
                Assert.That(errors, Is.Empty);
            }
            _provider.Verify();
        }

        [Test]
        public async Task ValidationService_TryValidateAsync_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var failures = new List<ValidationFailure>
            {
                new("Name", "Name is required"),
                new("Email", "Email is required"), // <--
                new("Email", "Invalid email format") // <--
            };

            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<TestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures))
                .Verifiable();

            // Act
            var (isValid, errors) = await _service.TryValidateAsync(new TestDto());

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.False);
                Assert.That(errors, Has.Count.EqualTo(2));
                Assert.That(errors["Email"], Has.Length.EqualTo(2));
            }
            _validator.Verify();
        }

        [Test]
        public async Task ValidationService_TryValidateAsync_ShouldCallValidatorOnce()
        {
            // Arrange
            _validator
                .Setup(v => v.ValidateAsync(It.IsAny<TestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult())
                .Verifiable();

            // Act
            await _service.TryValidateAsync(new TestDto());

            // Assert
            _validator.Verify(
                v => v.ValidateAsync(It.IsAny<TestDto>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion


    }

    public class TestDto
    {
        public string? Name { get; set; }
        public string Email { get; set; } = string.Empty;

    }
}