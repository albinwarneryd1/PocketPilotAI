using System.Net;
using System.Text.Json;
using PocketPilotAI.Contracts.Errors;

namespace PocketPilotAI.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
  public async Task Invoke(HttpContext context)
  {
    try
    {
      await next(context);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unhandled exception for request {Method} {Path}", context.Request.Method, context.Request.Path);

      context.Response.ContentType = "application/json";
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

      ApiError error = new()
      {
        Code = "internal_error",
        Message = "An unexpected server error occurred.",
        TraceId = context.TraceIdentifier
      };

      await context.Response.WriteAsync(JsonSerializer.Serialize(error));
    }
  }
}
