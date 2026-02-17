namespace PocketPilotAI.Api.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
  public async Task Invoke(HttpContext context)
  {
    DateTime started = DateTime.UtcNow;
    await next(context);
    TimeSpan elapsed = DateTime.UtcNow - started;

    logger.LogInformation(
      "HTTP {Method} {Path} => {StatusCode} in {ElapsedMs}ms",
      context.Request.Method,
      context.Request.Path,
      context.Response.StatusCode,
      elapsed.TotalMilliseconds);
  }
}
