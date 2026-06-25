using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.Exceptions;

namespace ProjectManagement.Api.Filters;

/// <summary>
/// Action filter for validating model state and handling validation errors.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var response = ApiResponse.ErrorResponse(
                "Validation failed",
                errors.SelectMany(e => e.Value.Select(v => new ApiError
                {
                    Code = "VALIDATION_ERROR",
                    Field = e.Key,
                    Message = v
                })).ToList()
            );

            context.Result = new BadRequestObjectResult(response);
            return;
        }

        await next();
    }
}
