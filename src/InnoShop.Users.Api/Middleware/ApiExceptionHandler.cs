using FluentValidation;
using InnoShop.Users.Api.Application.Exceptions;
using InnoShop.Users.Api.Composition;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Users.Api.Middleware;

public sealed class ApiExceptionHandler(RequestDelegate next, ILogger<ApiExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        IResult response;
        if (exception is ValidationException validation)
        {
            var errors = validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            response = Results.ValidationProblem(errors);
        }
        else if (exception is AppException known)
        {
            response = Results.Problem(statusCode: known.Status, title: "request failed", detail: known.Message);
        }
        else if (exception is BadHttpRequestException bad)
        {
            response = Results.Problem(statusCode: bad.StatusCode, title: "invalid request body or parameters");
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            response = Results.Problem(statusCode: 409, title: "data has changed, try again");
        }
        else if (exception is DbUpdateException update && update.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            response = Results.Problem(statusCode: 409, title: "value is already in use");
        }
        else
        {
            logger.LogError(exception, "request error");
            response = Results.Problem(statusCode: 500, title: "server error", detail: "something went wrong");
        }

        await response.ExecuteAsync(context);
    }
}
