using FluentValidation;
using MyBudgetIA.Application.Photo.Dtos;
using MyBudgetIA.Domain.Constraints;
using Shared.Helpers;

namespace MyBudgetIA.Application.Validators.Photo
{
    /// <summary>
    /// Validator for <see cref="PhotoUploadDto"/>.
    /// </summary>
    public class PhotoUploadDtoValidator : AbstractValidator<PhotoUploadDto>
    {
        /// <summary>
        /// Constructor taking the service(s) that this class depends on.
        /// <para/>Dynamically called by DI.
        /// </summary>
        public PhotoUploadDtoValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty()
                .WithName(nameof(PhotoUploadDto.FileName))
                .WithMessage(Messages.MustNotBeEmptyProperty);

            When(d => !string.IsNullOrWhiteSpace(d.FileName),
                () =>
                {
                    RuleFor(x => x.FileName)
                        .Must(fileName => !string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
                            .WithMessage(Messages.MustFileNameHaveExtension)
                        .Must(fileName => PhotoConstraints.IsValidFileName(fileName))
                            .WithMessage(Messages.MustFileNameCharactersValidInBlobContext)
                        .MaximumLength(PhotoConstraints.MaxFileNameLength)
                            .WithMessage(Messages.FileNameCannotExceedMaxLength);
                });

            RuleFor(d => d.ContentType)
                .NotEmpty()
                .WithName(nameof(PhotoUploadDto.ContentType))
                .WithMessage(Messages.MustNotBeEmptyProperty);

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
                .GreaterThan(0)
                .WithMessage(Messages.MustBeGreaterThan)
                .LessThanOrEqualTo(PhotoConstraints.MaxSizeInBytes)
                    .WithMessage(Messages.FileSizeCannotExceedMax);

            RuleFor(d => d.Content)
                .NotNull()
                .WithMessage(Messages.MustNotBeNullProperty);

            When(d => d.Content != null,
                () =>
                {
                    RuleFor(x => x.Content)
                    .Cascade(CascadeMode.Stop)
                    .Must(stream => stream.CanRead)
                        .WithMessage(Messages.MustStreamBeReadable)
                    .Must(stream => stream.CanSeek)
                    .WithMessage(Messages.MustStreamBeSeekable)
                    .Must((dto, content) => AreContentAndFileSameLength(dto, content))
                        .WithMessage(Messages.MustHaveSameContentAndFileLength);
                });

            RuleFor(d => d.Extension)
                .NotEmpty()
                .WithMessage(Messages.MustNotBeEmptyProperty);

            When(d => !string.IsNullOrWhiteSpace(d.Extension),
                () =>
                {
                    RuleFor(x => x.Extension)
                    .Must(ext => PhotoConstraints.AllowedExtensions.Contains(ext.ToLowerInvariant()))
                    .WithMessage(Messages.GetMustHaveValidExtensionMessage());
                });
        }

        private static bool IsContentTypeMatchingExtension(
            PhotoUploadDto candidateEntity,
            string? _)
        {
            var ext = candidateEntity.Extension.TrimStart('.').ToLowerInvariant();
            return PhotoConstraints.CorrespondingMimeTypes.TryGetValue(ext, out var allowed) &&
                allowed.Contains(candidateEntity.ContentType, StringComparer.InvariantCultureIgnoreCase);
        }

        private static bool AreContentAndFileSameLength(
            PhotoUploadDto candidateEntity,
            Stream? _) => candidateEntity.Content.Length == candidateEntity.Length;


        [ExposedOnlyToUnitTests]
        internal static class Messages
        {
            public const string MustNotBeEmptyProperty = "'{PropertyName}' can not be empty.";

            public const string MustFileNameHaveExtension = "File name must have an extension.";

            public const string MustFileNameBeUnderMaxLength = "'{PropertyName}' must be {MaxLength} characters max.";

            public const string MustNotBeNullProperty = "'{PropertyName}' can not be null.";

            public const string MustBeGreaterThan = "'{PropertyName}' must be greater than 0.";

            public const string MustHaveSameContentTypeAsExtension = "Content type does not match file extension.";

            public const string MustStreamBeReadable = "'{PropertyName}': Stream must be readable.";

            public const string MustStreamBeSeekable = "'{PropertyName}': Stream must be seekable.";

            //public const string MustStreamBeAtBeginning = "'{PropertyName}': Stream must be at the beginning.";

            public const string MustHaveSameContentAndFileLength = "Content stream length does not match file length.";

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