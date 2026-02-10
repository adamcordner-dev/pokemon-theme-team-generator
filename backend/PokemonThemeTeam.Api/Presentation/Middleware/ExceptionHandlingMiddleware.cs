using System.Net;
using System.Text.Json;
using PokemonThemeTeam.Api.Application.Models;

namespace PokemonThemeTeam.Api.Presentation.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var status = (int)HttpStatusCode.InternalServerError;

        var error = new ErrorResponse(
            Error: "An unexpected error occurred.",
            Details: ex.Message,
            StatusCode: status
        );

        var payload = JsonSerializer.Serialize(error);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        return context.Response.WriteAsync(payload);
    }
}
