using DotNetApiTemplate.Models.Response;
using SupermarketERP.Exceptions;
using System.Net;
using System.Text.Json;

namespace DotNetApiTemplate.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment hostEnvironment)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var response = context.Response;

        var errorResponse = new ApiResponse
        {
            IsSuccess = false
        };

        switch (exception)
        {
            case UserFriendlyException ex:
                errorResponse.Message = ex.Message;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = hostEnvironment.IsDevelopment() ? exception.Message : "Something went wrong.";
                break;
        }

        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = JsonSerializer.Serialize(errorResponse, options);
        await context.Response.WriteAsync(result);
    }
}