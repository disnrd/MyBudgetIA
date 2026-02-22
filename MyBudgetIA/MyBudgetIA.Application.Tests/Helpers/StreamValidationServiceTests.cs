using MyBudgetIA.Application.Exceptions;
using MyBudgetIA.Application.TechnicalServices;

namespace MyBudgetIA.Application.Tests.Helpers
{
    /// <summary>
    /// Units tests for class <see cref="StreamValidationService"/>.
    /// </summary>
    [TestFixture]
    public class StreamValidationServiceTests
    {

        private StreamValidationService service;

        [SetUp]
        public void SetUp()
        {
            service = new StreamValidationService();
        }

        [Test] public void StreamValidationService_ValidateStreamOrThrow_WhenStreamIsNull_Throws() 
        {
            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => service.ValidateStreamOrThrow(10, null!));
            Assert.That
                (ex.Errors.Values.SelectMany(v => v),
                Does.Contain(StreamValidationService.Messages.StreamValidation.StreamMustNotBeNull));
        }

        private class UnreadableStream : MemoryStream 
        { 
            public override bool CanRead => false;
        } 
        
        [Test] public void StreamValidationService_ValidateStreamOrThrow_WhenStreamNotReadable_Throws() 
        {
            // Arrange
            var stream = new UnreadableStream();

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => service.ValidateStreamOrThrow(10, stream));
            Assert.That
                (ex.Errors.Values.SelectMany(v => v),
                Does.Contain(StreamValidationService.Messages.StreamValidation.StreamMustBeReadable));
        }
        
        [Test] public void StreamValidationService_ValidateStreamOrThrow_WhenLengthMismatch_Throws() 
        {
            // Arrange
            var stream = new MemoryStream(new byte[5]);

            // Act & Assert
            var ex = Assert.Throws<ValidationException>(() => service.ValidateStreamOrThrow(10, stream));
            Assert.That
                (ex.Errors.Values.SelectMany(v => v),
                Does.Contain(StreamValidationService.Messages.StreamValidation.StreamLengthMustMatchProvidedLength));
        }
        
        [Test] public void StreamValidationService_ValidateStreamOrThrow_WhenPositionNotZero_NormalizesToZero() 
        {
            // Arrange
            var stream = new MemoryStream(new byte[10])
            {
                Position = 5
            };

            // Act & Assert
            Assert.DoesNotThrow(() => service.ValidateStreamOrThrow(10, stream));
            Assert.That(stream.Position, Is.EqualTo(0)); 
        } 
        
        [Test] public void StreamValidationService_ValidateStreamOrThrow_ValidStream_DoesNotThrow()
        {
            // Arrange
            var stream = new MemoryStream(new byte[10]);

            // Act & Assert
            Assert.DoesNotThrow(() => service.ValidateStreamOrThrow(10, stream)); 
        }
    }
}
