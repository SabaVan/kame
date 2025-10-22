using Microsoft.AspNetCore.Mvc;
using backend.Common;

namespace backend.Utils
{
    public static class ControllerExtensions
    {
        public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result, string? successMessage = null)
        {
            if (result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(successMessage))
                    return controller.Ok(successMessage);
                else
                    return controller.Ok(result.Value); // T can be returned directly
            }


            if (result.Error == null)
                return controller.Problem("Unknown error", statusCode: 500);

            return result.Error.Code switch
            {
                "NOT_FOUND" => controller.NotFound(result.Error.Message),
                "INVALID_ACTION" => controller.BadRequest(result.Error.Message),
                "ALREADY_EXISTS" => controller.Conflict(result.Error.Message),
                "UNAUTHORIZED" => controller.Unauthorized(result.Error.Message),
                "INVALID_INPUT" => controller.BadRequest(result.Error.Message),
                _ => controller.Problem(result.Error.Message, statusCode: 500)
            };
        }
    }
}
