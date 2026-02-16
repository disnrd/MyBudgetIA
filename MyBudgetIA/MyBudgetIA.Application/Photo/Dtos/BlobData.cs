namespace MyBudgetIA.Application.Photo.Dtos
{

    /// <summary>
    /// Represents basic information about a blob stored in Azure Blob Storage.
    /// </summary>
    /// <param name="BlobName">The unique name of the blob inside the container.</param>
    /// <param name="FileName">The original filename provided by the client, stored in blob metadata.</param>
    /// <param name="LastModified">The timestamp indicating when the blob was last modified in storage.</param>
    public record BlobData(
        string BlobName,
        string FileName,
        DateTimeOffset? LastModified
    );

}
