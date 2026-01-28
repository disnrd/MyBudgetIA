using System.Diagnostics.CodeAnalysis;

namespace Shared.Models
{
    public class ApiResponse<TData>
    {
        public bool Success { get; set; }

        [StringSyntax("json")]
        public string Message { get; set; } = string.Empty;

        public TData Data { get; set; } = default!;

        public ApiError[] Errors { get; set; } = [];

        public static ApiResponse<TData> Ok(TData data, string message = "Success")
            => new()
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = []
            };

        public static ApiResponse<TData> Fail(string message, ApiError[]? errors = null)
            => new()
            {
                Success = false,
                Message = message,
                Data = default!,
                Errors = errors ?? []
            };
    }

    public class ApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public ApiError[] Errors { get; set; } = [];

        public static ApiResponse Ok(string message = "Success")
            => new()
            {
                Success = true,
                Message = message,
                Errors = []
            };

        public static ApiResponse Fail(string message, ApiError[]? errors = null)
            => new()
            {
                Success = false,
                Message = message,
                Errors = errors ?? []
            };
    }
}
