using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectManagement.Application.Common.Models;
using ApplicationExceptions = ProjectManagement.Application.Exceptions;

namespace ProjectManagement.Api.Filters;

/// <summary>
/// Exception filter for handling application exceptions.
/// </summary>
public class ApplicationExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApplicationExceptionFilter> _logger;

    public ApplicationExceptionFilter(ILogger<ApplicationExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;

        (int statusCode, ApiResponse response) result;

        if (exception is ApplicationExceptions.ValidationException validationEx)
        {
            result = (
                StatusCodes.Status400BadRequest,
                ApiResponse.ErrorResponse(
                    validationEx.Message,
                    validationEx.Errors.SelectMany(e =>
                        e.Value.Select(v => new ApiError
                        {
                            Code = validationEx.Code,
                            Field = e.Key,
                            Message = v
                        })
                    ).ToList()
                )
            );
        }
        else if (exception is ApplicationExceptions.NotFoundException notFoundEx)
        {
            result = (
                StatusCodes.Status404NotFound,
                ApiResponse.ErrorResponse(notFoundEx.Message, new List<ApiError>
                {
                    new() { Code = notFoundEx.Code, Message = notFoundEx.Details ?? notFoundEx.Message }
                })
            );
        }
        else if (exception is ApplicationExceptions.BusinessRuleException businessEx)
        {
            result = (
                StatusCodes.Status422UnprocessableEntity,
                ApiResponse.ErrorResponse(businessEx.Message, new List<ApiError>
                {
                    new() { Code = businessEx.Code, Message = businessEx.Message }
                })
            );
        }
        else if (exception is ApplicationExceptions.AlreadyExistsException alreadyExistsEx)
        {
            result = (
                StatusCodes.Status409Conflict,
                ApiResponse.ErrorResponse(alreadyExistsEx.Message, new List<ApiError>
                {
                    new() { Code = alreadyExistsEx.Code, Message = alreadyExistsEx.Details ?? alreadyExistsEx.Message }
                })
            );
        }
        else if (exception is ApplicationExceptions.ApplicationException appEx)
        {
            result = (
                StatusCodes.Status400BadRequest,
                ApiResponse.ErrorResponse(appEx.Message, new List<ApiError>
                {
                    new() { Code = appEx.Code, Message = appEx.Message }
                })
            );
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred");
            result = (
                StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse("An unexpected error occurred. Please try again later.")
            );
        }

        context.ExceptionHandled = true;
        context.Result = new ObjectResult(result.response)
        {
            StatusCode = result.statusCode
        };
    }
}