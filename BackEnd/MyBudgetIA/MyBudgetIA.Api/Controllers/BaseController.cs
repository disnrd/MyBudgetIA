using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace MyBudgetIA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class BaseController() : ControllerBase
    {
        protected IActionResult OkResponse<T>(T data, string message = "Success")
            => Ok(ApiResponse<T>.Ok(data, message));

        protected IActionResult OkResponse(string message = "Success")
            => Ok(ApiResponse.Ok(message));

        protected IActionResult BadRequestResponse<T>(string message, ApiError[]? errors = null)
            => BadRequest(ApiResponse<T>.Fail(message, errors));

        protected IActionResult BadRequestResponse(string message, ApiError[]? errors = null)
            => BadRequest(ApiResponse.Fail(message, errors));

        protected IActionResult NotFoundResponse<T>(string message, ApiError[]? errors = null)
            => NotFound(ApiResponse<T>.Fail(message, errors));

        protected IActionResult NotFoundResponse(string message, ApiError[]? errors = null)
            => NotFound(ApiResponse.Fail(message, errors));
    }
}
