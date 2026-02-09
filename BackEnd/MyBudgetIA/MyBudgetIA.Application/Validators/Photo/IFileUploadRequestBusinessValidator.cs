using FluentValidation;
using MyBudgetIA.Application.Photo;
using MyBudgetIA.Domain.Constraints;
using Shared.Helpers;

namespace MyBudgetIA.Application.Validators.Photo
{
    /// <summary>
    /// /// Provides business validation rules for <see cref="IFileUploadRequest"/> instances.
    /// </summary>
    public class IFileUploadRequestBusinessValidator : AbstractValidator<IFileUploadRequest>
    {
        /// <summary>
        /// Constructor taking the service(s) that this class depends on.
        /// <para/>Dynamically called by DI.
        /// </summary>
        public IFileUploadRequestBusinessValidator()
        {
            When(d => !string.IsNullOrWhiteSpace(d.FileName),
                () =>
                {
                    RuleFor(x => x.FileName)
                        .Must(fileName => PhotoConstraints.IsValidFileName(fileName))
                            .WithMessage(Messages.MustFileNameCharactersValidInBlobContext)
                        .MaximumLength(PhotoConstraints.MaxFileNameLength)
                            .WithMessage(Messages.FileNameCannotExceedMaxLength);
                });

            When(d => !string.IsNullOrWhiteSpace(d.ContentType),
                () =>
                {
                    RuleFor(x => x.ContentType)
                    .Must((dto, contentType) => IsContentTypeMatchingExtension(dto, contentType))
                        .WithMessage(Messages.MustHaveSameContentTypeAsExtension)
                        .When(p => !string.IsNullOrWhiteSpace(p.Extension))
                    .Must(ct => PhotoConstraints.AllowedContentTypes.Contains(ct.ToLowerInvariant()))
                        .WithMessage(Messages.GetMustHaveValidContentTypeMessage());
                });

            RuleFor(d => d.Length)
                .LessThanOrEqualTo(PhotoConstraints.MaxSizeInBytes)
                    .WithMessage(Messages.FileSizeCannotExceedMax);

            When(d => !string.IsNullOrWhiteSpace(d.Extension),
                () =>
                {
                    RuleFor(x => x.Extension)
                    .Must(ext => PhotoConstraints.AllowedExtensions.Contains(ext.ToLowerInvariant()))
                    .WithMessage(Messages.GetMustHaveValidExtensionMessage());
                });
        }

        private static bool IsContentTypeMatchingExtension(
            IFileUploadRequest candidateEntity,
            string? _)
        {
            var ext = candidateEntity.Extension.TrimStart('.').ToLowerInvariant();
            return PhotoConstraints.CorrespondingMimeTypes.TryGetValue(ext, out var allowed) &&
                allowed.Contains(candidateEntity.ContentType, StringComparer.InvariantCultureIgnoreCase);
        }

        [ExposedOnlyToUnitTests]
        internal static class Messages
        {
            public const string MustHaveSameContentTypeAsExtension = "Content type does not match file extension.";

            public const string MustFileNameCharactersValidInBlobContext = "File name contains at least one of the following invalid characters for blob storage " +
                ": '\"', '/', ':', '|', '<', '>', '*', '?','\x00-','\x1F','\x7F']'";

            public const string MustHaveValidExtensionTemplate = "Extension must be one of: {0}";

            public static readonly string FileSizeCannotExceedMax = $"File size cannot exceed {PhotoConstraints.MaxSizeInBytes}MB";

            public static readonly string FileNameCannotExceedMaxLength =
                $"File name cannot exceed {PhotoConstraints.MaxFileNameLength} characters.";

            public static string GetMustHaveValidExtensionMessage()
            {
                return string.Format(MustHaveValidExtensionTemplate,
                    string.Join(", ", PhotoConstraints.AllowedExtensions));
            }

            public const string MustHaveValidContentTypeTemplate = "Content type must be one of: {0}";

            public static string GetMustHaveValidContentTypeMessage()
            {
                return string.Format(MustHaveValidContentTypeTemplate,
                    string.Join(", ", PhotoConstraints.AllowedContentTypes));
            }
        }
    }
}
