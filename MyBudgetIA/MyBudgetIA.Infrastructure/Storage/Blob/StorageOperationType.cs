namespace MyBudgetIA.Infrastructure.Storage.Blob
{
    /// <summary>
    /// Specifies the set of operations that can be performed on an azure storage.
    /// </summary>
    public enum StorageOperationType
    {
        /// <summary>
        /// Represents an operation that uploads data to a specified destination.
        /// </summary>
        BlobUpload,

        /// <summary>
        /// Gets or sets a value indicating whether the operation represents a download action.
        /// </summary>
        BlobDownload,

        /// <summary>
        /// Represents a message that is sent to a queue for processing.
        /// </summary>
        QueueMessageSending,

        /// <summary>
        /// Represents a message that is received from a queue, containing the message content and metadata.
        /// </summary>
        QueueMessageReceiving,

        //Delete,
        //List,
        //GetMetadata
    }
}
