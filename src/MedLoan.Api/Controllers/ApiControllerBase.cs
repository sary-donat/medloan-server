using MedLoan.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error, result.Message);

    // PUT / PATCH / DELETE: success has no body.
    protected IActionResult ToActionResult(Result result) =>
        result.IsSuccess ? NoContent() : ToProblem(result.Error, result.Message);

    // POST: 201 with a Location header that points to actionName.
    protected IActionResult ToCreatedResult<T>(Result<T> result, string actionName, Func<T, object> routeValues) =>
        result.IsSuccess
            ? CreatedAtAction(actionName, routeValues(result.Value!), result.Value)
            : ToProblem(result.Error, result.Message);

    private ObjectResult ToProblem(ErrorType error, string? message)
    {
        var status = error switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, "A failed result must have an error type.")
        };

        return Problem(detail: message, statusCode: status);
    }
}
