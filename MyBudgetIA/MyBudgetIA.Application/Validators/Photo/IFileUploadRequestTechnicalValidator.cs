using FluentValidation;
using MyBudgetIA.Application.Photo;
using Shared.Helpers;

namespace MyBudgetIA.Application.Validators.Photo
{
    /// <summary>
    /// Provides technical validation rules for <see cref="IFileUploadRequest"/> instances.
    /// </summary>
    /// <remarks>This validator is intended to ensure that file upload requests meet required technical
    /// criteria before processing. It can be used to enforce constraints such as file size limits, required fields, or
    /// format validation as part of the request handling pipeline.</remarks>
    public class IFileUploadRequestTechnicalValidator : AbstractValidator<IFileUploadRequest>
    {
        /// <summary>
        /// Constructor taking the service(s) that this class depends on.
        /// <para/>Dynamically called by DI.
        /// </summary>
        public IFileUploadRequestTechnicalValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty()
                .WithName(nameof(IFileUploadRequest.FileName))
                .WithMessage(Messages.MustNotBeEmptyProperty);

            When(d => !string.IsNullOrWhiteSpace(d.FileName),
                () =>
                {
                    RuleFor(x => x.FileName)
                        .Must(fileName => !string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
                            .WithMessage(Messages.MustFileNameHaveExtension);
                });

            RuleFor(d => d.ContentType)
                .NotEmpty()
                .WithName(nameof(IFileUploadRequest.ContentType))
                .WithMessage(Messages.MustNotBeEmptyProperty);

            RuleFor(d => d.Length)
                .GreaterThan(0)
                .WithMessage(Messages.MustBeGreaterThan);

            RuleFor(d => d.Extension)
                .NotEmpty()
                .WithMessage(Messages.MustNotBeEmptyProperty);
        }
       
        [ExposedOnlyToUnitTests]
        internal static class Messages
        {
            public const string MustNotBeEmptyProperty = "'{PropertyName}' can not be empty.";

            public const string MustFileNameHaveExtension = "File name must have an extension.";

            public const string MustNotBeNullProperty = "'{PropertyName}' can not be null.";

            public const string MustBeGreaterThan = "'{PropertyName}' must be greater than 0.";

            public const string MustHaveSameContentAndFileLength = "Content stream length does not match file length.";

        }
    }
}
