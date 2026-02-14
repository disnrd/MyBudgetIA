namespace MyBudgetIA.Infrastructure.Storage
{
    /// <summary>
    /// Specifies the set of operations that can be performed on a blob in storage.
    /// </summary>
    public enum BlobOperationType
    {
        /// <summary>
        /// Represents an operation that uploads data to a specified destination.
        /// </summary>
        Upload,

        /// <summary>
        /// Gets or sets a value indicating whether the operation represents a download action.
        /// </summary>
        Download,

        //Delete,
        //List,
        //GetMetadata
    }
}
