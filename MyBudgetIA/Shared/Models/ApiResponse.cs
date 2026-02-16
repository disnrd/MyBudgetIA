namespace Shared.Models
{
    public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    ApiError[] Errors)
    {
        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new(true, message, data, []);

        public static ApiResponse<T> Fail(string message, ApiError[]? errors = null) =>
            new(false, message, default, errors ?? []);
    }

    public record ApiResponse(
        bool Success,
        string Message,
        ApiError[] Errors)
    {
        public static ApiResponse Ok(string message = "Success") =>
            new(true, message, []);

        public static ApiResponse Fail(string message, ApiError[]? errors = null) =>
            new(false, message, errors ?? []);
    }
}
