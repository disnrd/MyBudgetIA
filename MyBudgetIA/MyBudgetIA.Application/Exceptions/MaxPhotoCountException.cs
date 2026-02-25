using Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyBudgetIA.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when the number of photos in a request exceeds the maximum allowed.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the MaxPhotoCountException class with the specified maximum allowed photos and
    /// the number of photos provided.
    /// </remarks>
    /// <remarks>Use this exception to indicate that a request has exceeded the allowed number of
    /// photos. The values provided can be used to inform the user or for logging purposes.</remarks>
    /// <param name="maxPhotosAllowed">The maximum number of photos permitted per request.</param>
    /// <param name="photosProvided">The number of photos that were provided in the request.</param>
    public sealed class MaxPhotoCountException(int maxPhotosAllowed, int photosProvided) : ApplicationException(
            publicMessage: $"Too many photos provided. Maximum {maxPhotosAllowed} photos allowed per request, but {photosProvided} were provided",
            errorCode: ErrorCodes.MaxPhotoCountExceeded,
            statusCode: 400)
    {
        /// <summary>
        /// Gets the maximum number of photos that can be added or uploaded.
        /// </summary>
        public int MaxPhotosAllowed { get; } = maxPhotosAllowed;

        /// <summary>
        /// Gets the number of photos that have been provided.
        /// </summary>
        public int PhotosProvided { get; } = photosProvided;
    }
}
