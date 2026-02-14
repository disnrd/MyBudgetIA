using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using MyBudgetIA.Api.Middlewares;
using MyBudgetIA.Application.Exceptions;

namespace MyBudgetIA.Api.Tests
{
    /// <summary>
    /// Unit tests for class <see cref="ExceptionHandlingMiddleware"/>
    /// </summary>
    [TestFixture]
    public class ExceptionHandlingMiddlewareTests
    {
        #region SetUp

        private Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
        private Mock<IWebHostEnvironment> _mockEnvironment;
        private static readonly string[] value = ["File is required"];

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();

        }

        // Factory method to create middleware instance
        private ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
        {
            return new ExceptionHandlingMiddleware(
                next,
                _mockLogger.Object,
                _mockEnvironment.Object);
        }

        #endregion

        [Test]
        public async Task ExceptionHandlingMiddleware_InvokeAsync_WithValidationException_ShouldReturnBadRequest()
        {
            // Arrange
            var errors = new Dictionary<string, string[]>
            {
                { "file", value }
            };
            var exception = new ValidationException(errors);

            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/blobs/upload";
            context.Response.Body = new MemoryStream();

            async Task nextThrowingException(HttpContext ctx) => throw exception;

            // use the factory with the delegate that throws the exception
            var middleware = CreateMiddleware(nextThrowingException);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.That(context.Response.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task ExceptionHandlingMiddleware_InvokeAsync_WithValidRequest_ShouldCallNext()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var nextCalled = false;

            async Task validNext(HttpContext ctx)
            {
                nextCalled = true;
                ctx.Response.StatusCode = 200;
            }

            var middleware = CreateMiddleware(validNext);

            // Act
            await middleware.InvokeAsync(context);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(nextCalled, Is.True);
                Assert.That(context.Response.StatusCode, Is.EqualTo(200));
            }
        }
    }
}
