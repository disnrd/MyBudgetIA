using MyBudgetIA.Application.Exceptions;
using Serilog.Context;
using Shared.Helpers;
using Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyBudgetIA.Api.Middlewares
{
    public class ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                // todo : log exception details add serilog, create constant, unit test
                using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
                {
                    logger.LogError(
                        ex,
                        ExceptionMessages.LogMessages.UnhandledException_1,
                        ex.GetType().Name,
                        ex.Message,
                        ex.StackTrace);

                    await HandleExceptionAsync(context, ex);
                }
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var traceId = context.TraceIdentifier;
            var (statusCode, message, errors) = MapException(exception);
            context.Response.StatusCode = statusCode;

            using (LogContext.PushProperty("TraceId", traceId))
            using (LogContext.PushProperty("StatusCode", statusCode))
            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            {
                if (statusCode >= 500)
                {
                    logger.LogError(
                        ExceptionMessages.LogMessages.ServerErrorDetails,
                        traceId,
                        exception.GetType().Name,
                        message);
                }
                else
                {
                    logger.LogWarning(
                        ExceptionMessages.LogMessages.ClientError,
                        statusCode,
                        traceId,
                        message);
                }

                // log for Fluent validation errors
                if (exception is ValidationException validationEx)
                {
                    logger.LogWarning(
                        ExceptionMessages.LogMessages.ValidationError,
                        validationEx.Errors);
                }
            }

            var response = new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // TODO : finish tests coverage if possible ??

            try
            {
                await context.Response.WriteAsJsonAsync(response, jsonOptions);
            }
            catch (Exception writeEx)
            {
                using (LogContext.PushProperty("TraceId", traceId))
                {
                    logger.LogError(
                        writeEx,
                        ExceptionMessages.LogMessages.ResponseWriteError,
                        traceId);
                }
            }
        }

        private (int StatusCode, string Message, ApiError[] Errors) MapException(Exception exception)
            => exception switch
            {
                ValidationException validationEx => (
                    validationEx.StatusCode,
                    validationEx.Message,
                    validationEx.Errors
                        .SelectMany(kvp => kvp.Value.Select(msg => new ApiError
                        {
                            Code = ErrorCodes.ValidationError,
                            Field = kvp.Key,
                            Message = msg
                        }))
                        .ToArray()
                ),

                Application.Exceptions.ApplicationException appEx => (
                    appEx.StatusCode,
                    appEx.PublicMessage,
                    new[] { new ApiError { Code = appEx.ErrorCode, Message = appEx.PublicMessage } }
                ),

                Infrastructure.Exceptions.InfrastructureException infraEx => (
                    infraEx.StatusCode,
                    environment.IsDevelopment()
                        ? infraEx.PublicMessage
                        : ExceptionMessages.ErrorMessages.InternalServerError,
                    environment.IsDevelopment()
                        ? new[] { new ApiError { Code = infraEx.ErrorCode, Message = infraEx.PublicMessage } }
                        : []
                ),

                _ => (
                    500,
                    environment.IsDevelopment()
                        ? exception.Message
                        : ExceptionMessages.ErrorMessages.InternalServerError,
                    environment.IsDevelopment()
                        ? [new ApiError { Code = ErrorCodes.InternalError, Message = exception.StackTrace ?? string.Empty }]
                        : []
                )
            };

        [ExposedOnlyToUnitTests]
        internal static class ExceptionMessages
        {
            public static class LogMessages
            {
                public const string UnhandledException_1 = "Unhandled exception occurred: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}";
                public const string ValidationError = "StreamValidation failed for request: {@ValidationErrors}";
                public const string ServerError = "Server error [TraceId: {TraceId}]: {Message}";
                public const string ServerErrorDetails = "Server error [TraceId: {TraceId}]: {ExceptionType} - {Message}";
                public const string ClientError = "Client error ({StatusCode}) [TraceId: {TraceId}]: {Message}";
                public const string ResponseWriteError = "Error writing exception response to client [TraceId: {TraceId}]";
            }

            public static class ErrorMessages
            {
                public const string InternalServerError = "An internal error occurred";
            }
        }
    }
}
