namespace MyBudgetIA.Application.TechnicalServices
{
    /// <summary>
    /// Technical services related to stream validation.
    /// </summary>
    public interface IStreamValidationService
    {
        /// <summary>
        /// Validates or Throws an exception if the stream is not suitable for processing.
        /// </summary>
        /// <param name="fileLength">The expected length of the file, in bytes. Must be non-negative and equal to the length of the stream.</param>
        /// <param name="stream">The stream to validate. Must be open and readable; otherwise, an exception is thrown.</param>
        void ValidateStreamOrThrow(long fileLength, Stream stream);
    }
}
