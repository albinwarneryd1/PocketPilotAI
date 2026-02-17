namespace PocketPilotAI.Api.OpenApi;

public static class SwaggerExtensions
{
  public static IServiceCollection AddPocketPilotSwagger(this IServiceCollection services)
  {
    services.AddSwaggerGen();

    return services;
  }
}
