using MyBudgetIA.Application.Helpers;
using MyBudgetIA.Application.Photo;
using Shared.Helpers;

namespace MyBudgetIA.Application.TechnicalServices
{
    /// <inheritdoc cref="IStreamValidationService"/>
    public class StreamValidationService : IStreamValidationService
    {
        /// <inheritdoc/>
        public void ValidateStreamOrThrow(long fileLength, Stream stream)
        {
            var errors = new ValidationErrors();
            var field = nameof(IFileUploadRequest.OpenReadStream);

            ValidateNull(stream, errors, field);
            ValidateReadable(stream, errors, field);
            NormalizePosition(stream);
            ValidateLength(stream, fileLength, errors, field);

            errors.ThrowIfAny();
        }

        private static void ValidateNull(Stream stream, ValidationErrors errors, string field)
        {
            if (stream is null)
                errors.Add(field, Messages.StreamValidation.StreamMustNotBeNull);
                errors.ThrowIfAny();
        }

        private static void ValidateReadable(Stream stream, ValidationErrors errors, string field)
        {
            if (!stream.CanRead)
            {
                errors.Add(field, Messages.StreamValidation.StreamMustBeReadable);
            }
        }

        private static void NormalizePosition(Stream stream)
        {
            if (stream.CanSeek && stream.Position != 0)
            {
                stream.Position = 0;
            }
        }

        private static void ValidateLength(Stream stream, long fileLength, ValidationErrors errors, string field)
        {
            if (stream.CanSeek && stream.Length != fileLength)
            {
                errors.Add(field, Messages.StreamValidation.StreamLengthMustMatchProvidedLength);
            }
        }




        [ExposedOnlyToUnitTests]
        internal static class Messages
        {
            public static class StreamValidation
            {
                public const string StreamMustNotBeNull = "Stream can not be null.";
                public const string StreamMustBeReadable = "Stream must be readable.";
                public const string StreamLengthMustMatchProvidedLength = "Stream length does not match file length.";
            }
        }
    }
}
