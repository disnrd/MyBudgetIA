namespace Shared.Models
{
    public static class ApiErrors
    {
        public static ApiError Required(string field) =>
            new()
            {
                Code = ErrorCodes.BadRequest,
                Field = field,
                Message = $"{field} is required."
            };

        public static ApiError RequiredWithMessage(string field, string message) =>
            new()
            {
                Code = ErrorCodes.BadRequest,
                Field = field,
                Message = message
            };

        public static ApiError NotFound(string field, string message) =>
            new()
            {
                Code = ErrorCodes.NotFound,
                Field = field,
                Message = message
            };
    }
}
