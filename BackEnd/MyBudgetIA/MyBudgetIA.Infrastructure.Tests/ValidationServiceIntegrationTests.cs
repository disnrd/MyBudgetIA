using FluentValidation;
using FluentValidation.Results;
using k8s.Models;
using MyBudgetIA.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;

namespace MyBudgetIA.Infrastructure.Tests
{
    /// <summary>
    /// Integration tests for class <see cref="ValidationService"/>
    /// </summary>
    [TestFixture]
    public class ValidationServiceIntegrationTests
    {
        // TODO : rajouter tests pour ValidateAndThrowAllAsync

        private IServiceProvider _serviceProvider;
        private ValidationService _validationService;
        private IServiceCollection _services;

        #region SetUp

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();

            _services.AddScoped<IValidator<TestDto>, TestDtoValidator>();
            _services.AddScoped<ValidationService>();

            _serviceProvider = _services.BuildServiceProvider();
            _validationService = _serviceProvider.GetRequiredService<ValidationService>();
        }

        #endregion

        #region ValidateAndThrowAsync

        [Test]
        public async Task ValidationService_ValidateAndThrowAsync_WithValidDto_ShouldNotThrow()
        {
            // Arrange
            var dto = new TestDto { Name = "John", Email = "john@example.com" };

            // Act & Assert
            Assert.DoesNotThrowAsync(() => _validationService.ValidateAndThrowAsync(dto));
        }

        [Test]
        public async Task ValidationService_ValidateAndThrowAsync_WithInvalidDto_ShouldThrow()
        {
            // Arrange
            var dto = new TestDto { Name = "", Email = "" }; // Invalid

            // Act & Assert
            Assert.ThrowsAsync<Application.Exceptions.ValidationException>(
                () => _validationService.ValidateAndThrowAsync(dto));
        }

        [Test]
        public async Task ValidationService_ValidateAndThrowAsync_WithMultipleErrors_ShouldThrowWithAllErrors()
        {
            // Arrange
            var dto = new TestDto { Name = "Jo", Email = "email" }; // <-- 2 errors

            // Act
            var ex = Assert.ThrowsAsync<Application.Exceptions.ValidationException>(async () =>
                await _validationService.ValidateAndThrowAsync(dto));

            // Assert

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex.Errors, Has.Count.EqualTo(2));
                Assert.That(ex.Errors.ContainsKey(nameof(TestDto.Name)), Is.True);
                Assert.That(ex.Errors.ContainsKey(nameof(TestDto.Email)), Is.True);
                Assert.That(ex.Errors["Name"], Contains.Item("Name must be at least 3 characters"));
                Assert.That(ex.Errors["Email"], Contains.Item("Invalid email format"));
            }
        }

        #endregion

        #region TryValidateAsync

        [Test]
        public async Task ValidationService_TryValidateAsync_WithValidDto_ShouldReturnTrueAndEmptyErrors()
        {
            // Arrange
            var dto = new TestDto { Name = "John Doe", Email = "john@example.com" };

            // Act
            var (isValid, errors) = await _validationService.TryValidateAsync(dto);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.True);
                Assert.That(errors, Is.Empty);
            }
        }

        [Test]
        public async Task ValidationService_TryValidateAsync_WithInvalidDto_ShouldReturnFalseAndErrors()
        {
            // Arrange
            var dto = new TestDto { Name = "", Email = "" };

            // Act
            var (isValid, errors) = await _validationService.TryValidateAsync(dto);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.False);
                Assert.That(errors, Is.Not.Empty);
                Assert.That(errors.ContainsKey("Name"), Is.True);
                Assert.That(errors.ContainsKey("Email"), Is.True);
            }
        }

        [Test]
        public async Task ValidationService_TryValidateAsync_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var dto = new TestDto { Name = "Jo", Email = "invalid" };

            // Act
            var (isValid, errors) = await _validationService.TryValidateAsync(dto);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.False);
                Assert.That(errors, Has.Count.EqualTo(2)); // Name et Email (exactement 2)
                Assert.That(errors.ContainsKey(nameof(TestDto.Name)), Is.True);
                Assert.That(errors.ContainsKey(nameof(TestDto.Email)), Is.True);

                Assert.That(errors["Name"], Contains.Item("Name must be at least 3 characters"));
                Assert.That(errors["Email"], Contains.Item("Invalid email format"));
            }

        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        // Validator
        public class TestDtoValidator : AbstractValidator<TestDto>
        {
            public TestDtoValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Name is required")
                    .MinimumLength(3).WithMessage("Name must be at least 3 characters");

                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required")
                    .EmailAddress().WithMessage("Invalid email format");
            }
        }

        // Test DTO
        public class TestDto
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }
    }
}
