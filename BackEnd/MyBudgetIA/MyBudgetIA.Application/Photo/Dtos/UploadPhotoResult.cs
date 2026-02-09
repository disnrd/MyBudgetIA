namespace MyBudgetIA.Application.Photo.Dtos
{
    /// <summary>
    /// Represents the overall outcome of an upload operation for multiple photos.
    /// </summary>
    /// <remarks>
    /// This type is designed to support partial success scenarios where some files may upload successfully while
    /// others fail.
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="UploadPhotosResult"/> class.
    /// </remarks>
    /// <param name="items">The per-file upload results.</param>
    public sealed class UploadPhotosResult(IReadOnlyCollection<BlobUploadResult> items)
    {
        /// <summary>
        /// Gets the per-file upload results.
        /// </summary>
        public IReadOnlyCollection<BlobUploadResult> Items { get; } = items ?? throw new ArgumentNullException(nameof(items));

        /// <summary>
        /// Gets a value indicating whether all uploads succeeded.
        /// </summary>
        public bool IsSuccess => Items.Count > 0 && Items.All(i => i.IsSuccess);

        /// <summary>
        /// Gets a value indicating whether at least one upload succeeded and at least one upload failed.
        /// </summary>
        public bool IsPartialSuccess => Items.Any(i => i.IsSuccess) && Items.Any(i => !i.IsSuccess);
    }
}