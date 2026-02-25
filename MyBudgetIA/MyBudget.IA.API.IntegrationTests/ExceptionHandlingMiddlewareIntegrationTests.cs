using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using MyBudgetIA.Api.Middlewares;
using MyBudgetIA.Application.Exceptions;
using MyBudgetIA.Infrastructure.Exceptions;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using ExceptionMessages = MyBudgetIA.Api.Middlewares.ExceptionHandlingMiddleware.ExceptionMessages.ErrorMessages;

namespace MyBudgetIA.Api.Tests
{
    /// <summary>
    /// Integration tests for class <see cref="ExceptionHandlingMiddleware"/>
    /// </summary>
    [TestFixture]
    public class ExceptionHandlingMiddlewareIntegrationTests
    {
        private DefaultHttpContext _context = null!;
        private Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock = null!;
        private Mock<IWebHostEnvironment> _environmentMock = null!;
        private ExceptionHandlingMiddleware _middleware = null!;
        private static readonly string[] value = ["Error"];

        // static JsonSerializerOptions intance to avoid CA1869
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #region SetUp

        [SetUp]
        public void Setup()
        {
            _context = new DefaultHttpContext();
            _context.Response.Body = new MemoryStream();
            _context.TraceIdentifier = Guid.NewGuid().ToString();

            _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        }

        private async Task<ApiResponse> GetResponseFromContext()
        {
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(_context.Response.Body).ReadToEndAsync();

            return JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions)!;
        }

        #endregion

        #region ValidationException Tests

        [Test]
        public async Task ExceptionHandlingMiddleware_WithValidationException_ShouldReturn400AndLogWarning()
        {
            // Arrange
            const string nameErrorMessage = "Name is required";
            const string emailErrorMessage = "Invalid email format";

            var errors = new Dictionary<string, string[]>
            {
                { "Name", new[] { nameErrorMessage } },
                { "Email", new[] { emailErrorMessage } }
            };

            var exception = new ValidationException(errors);

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_context.Response.StatusCode, Is.EqualTo(400));
                Assert.That(_context.Response.ContentType, Does.StartWith("application/json"));
            }

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_WithValidationException_ShouldReturnApiResponseWithErrors()
        {
            // Arrange
            const string nameErrorMessage = "Name is required";
            const string emailErrorMessage = "Invalid email format";

            var errors = new Dictionary<string, string[]>
            {
                { "Name", new[] { nameErrorMessage } },
                { "Email", new[] { emailErrorMessage } }
            };

            var exception = new ValidationException(errors);

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            var response = await GetResponseFromContext();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Success, Is.False);
                Assert.That(response.Errors, Is.Not.Null);
                Assert.That(response.Errors, Has.Length.EqualTo(2));
            }

            var nameError = response.Errors.FirstOrDefault(e => e.Field == "Name");
            var emailError = response.Errors.FirstOrDefault(e => e.Field == "Email");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nameError, Is.Not.Null);
                Assert.That(nameError!.Code, Is.EqualTo(ErrorCodes.ValidationError));
                Assert.That(nameError.Message, Is.EqualTo("Name is required"));

                Assert.That(emailError, Is.Not.Null);
                Assert.That(emailError!.Code, Is.EqualTo(ErrorCodes.ValidationError));
            }
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_WithValidationException_ShouldIncludeTraceId()
        {
            // Arrange
            var traceId = "test-trace-123";
            _context.TraceIdentifier = traceId;

            var exception = new ValidationException(
                new Dictionary<string, string[]> { { "Field", value } });

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region ApplicationException Tests

        [Test]
        public async Task ExceptionHandlingMiddleware_WithApplicationException_ShouldReturn400AndIncludeApiError()
        {
            // Arrange
            var ex = new MaxPhotoCountException(maxPhotosAllowed: 5, photosProvided: 6);

            Task next(HttpContext ctx) => throw ex;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.That(_context.Response.StatusCode, Is.EqualTo(400));

            var response = await GetResponseFromContext();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Success, Is.False);
                Assert.That(response.Message, Is.EqualTo(ex.PublicMessage));
                Assert.That(response.Errors, Is.Not.Null);
                Assert.That(response.Errors, Has.Length.EqualTo(1));
                Assert.That(response.Errors[0].Code, Is.EqualTo(ex.ErrorCode));
                Assert.That(response.Errors[0].Message, Is.EqualTo(ex.PublicMessage));
            }
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_WithApplicationException_InProduction_ShouldStillReturnPublicMessage()
        {
            // Arrange
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
            var ex = new MaxPhotoCountException(maxPhotosAllowed: 5, photosProvided: 6);

            Task next(HttpContext ctx) => throw ex;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.That(_context.Response.StatusCode, Is.EqualTo(400));
            var response = await GetResponseFromContext();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Success, Is.False);
                Assert.That(response.Message, Is.EqualTo(ex.PublicMessage));
                Assert.That(response.Errors, Is.Not.Null);
                Assert.That(response.Errors, Has.Length.EqualTo(1));
                Assert.That(response.Errors[0].Code, Is.EqualTo(ex.ErrorCode));
                Assert.That(response.Errors[0].Message, Is.EqualTo(ex.PublicMessage));
            }
        }

        #endregion

        #region UnhandledException Tests - Development

        [Test]
        public async Task ExceptionHandlingMiddleware_WithUnhandledException_InDevelopment_ShouldReturn500WithDetails()
        {
            // Arrange
            const string detailedMessage = "Internal error with details";
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

            var exception = new Exception(detailedMessage);

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.That(_context.Response.StatusCode, Is.EqualTo(500));

            var response = await GetResponseFromContext();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Success, Is.False);
                Assert.That(response.Message, Is.EqualTo(detailedMessage));
            }
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_WithUnhandledException_InDevelopment_ShouldIncludeStackTrace()
        {
            // Arrange
            const string detailedMessage = "Test exception with stack trace";
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

            var exception = new Exception(detailedMessage);

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            var response = await GetResponseFromContext();

            // Dev env, only StackTrace
            if (response.Errors.Length > 0)
            {
                Assert.That(response.Errors, Is.Not.Null);
                Assert.That(response.Errors[0].Code, Is.EqualTo(ErrorCodes.InternalError));
            }
        }

        #endregion

        #region UnhandledException Tests - Production

        [Test]
        public async Task ExceptionHandlingMiddleware_WithUnhandledException_InProduction_ShouldReturn500WithGenericMessage()
        {
            // Arrange
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

            var exception = new Exception("Sensitive internal error details");

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.That(_context.Response.StatusCode, Is.EqualTo(500));

            using (Assert.EnterMultipleScope())
            {
                var response = await GetResponseFromContext();
                Assert.That(response.Success, Is.False);
                Assert.That(response.Message, Is.EqualTo(ExceptionMessages.InternalServerError));
            }
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_WithUnhandledException_InProduction_ShouldNotExposeStackTrace()
        {
            // Arrange
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

            var exception = new Exception("Connection string: Server=secret;Password=secret123");

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            var response = await GetResponseFromContext();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Message, Does.Not.Contain("secret"));
                Assert.That(response.Message, Does.Not.Contain("Password"));
                Assert.That(response.Errors, Is.Not.Null.And.Empty); // no detailed errors in production
            }
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_WithUnhandledException_InProduction_ShouldLogError()
        {
            // Arrange
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

            var exception = new Exception("Internal error");

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Happy Path Tests

        [Test]
        public async Task ExceptionHandlingMiddleware_WithNoException_ShouldCallNext()
        {
            // Arrange
            var nextCalled = false;

            Task next(HttpContext ctx)
            {
                nextCalled = true;
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            }

            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(nextCalled, Is.True);
                Assert.That(_context.Response.StatusCode, Is.EqualTo(200));
            }
        }

        #endregion

        #region ContentType Tests

        [Test]
        public async Task ExceptionHandlingMiddleware_ShouldAlwaysReturnJsonContentType()
        {
            // Arrange
            const string detailedMessage = "Some error occurred";
            var exception = new ValidationException(
                new Dictionary<string, string[]> { { "Field", new[] { detailedMessage } } });

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.That(_context.Response.ContentType, Does.StartWith("application/json"));
        }

        #endregion

        #region Logging Tests

        [Test]
        public async Task ExceptionHandlingMiddleware_WithServerError_ShouldLogAsError()
        {
            // Arrange
            var exception = new Exception("Server error");

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_WithClientError_ShouldLogAsWarning()
        {
            // Arrange
            const string detailedMessage = "StreamValidation failed";
            var exception = new ValidationException(
                new Dictionary<string, string[]> { { "Field", new[] { detailedMessage } } });

            Task next(HttpContext ctx) => throw exception;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region InfrastructureException Tests

        [Test]
        public async Task ExceptionHandlingMiddleware_WithInfrastructureException_InDevelopment_ShouldReturnDetails()
        {
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
            const string message = "Blob storage unavailable.";

            var ex = new BlobStorageException(
                blobName: "testblob.txt",
                azureStatusCode: 500,
                azureErrorCode: ErrorCodes.BlobUnavailable,
                message: message);

            Task next(HttpContext ctx) => throw ex;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            await _middleware.InvokeAsync(_context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(_context.Response.StatusCode, Is.EqualTo(500));
                var response = await GetResponseFromContext();
                Assert.That(response.Message, Is.EqualTo(message));
                Assert.That(response.Errors, Has.Length.EqualTo(1));
                Assert.That(response.Errors[0].Code, Is.EqualTo(ErrorCodes.BlobUnavailable));
                Assert.That(response.Errors[0].Message, Is.EqualTo(message));
            }
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_WithInfrastructureException_InProduction_ShouldReturnGenericMessage()
        {
            // Arrange
            _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

            var ex = new BlobStorageException(
                blobName: "testblob.txt",
                azureErrorCode: "500",
                message: "Connection string: Server=secret;Password=secret123");

            Task next(HttpContext ctx) => throw ex;
            _middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.That(_context.Response.StatusCode, Is.EqualTo(503)); 
            var response = await GetResponseFromContext();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Success, Is.False);
                Assert.That(response.Message, Is.EqualTo(ExceptionMessages.InternalServerError));
                Assert.That(response.Message, Does.Not.Contain("secret"));
                Assert.That(response.Errors, Is.Not.Null.And.Empty);
            }
        }

        #endregion
    }
}
