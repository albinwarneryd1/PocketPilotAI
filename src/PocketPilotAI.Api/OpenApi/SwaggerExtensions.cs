using Microsoft.OpenApi.Models;

namespace PocketPilotAI.Api.OpenApi;

public static class SwaggerExtensions
{
  public static IServiceCollection AddPocketPilotSwagger(this IServiceCollection services)
  {
    services.AddSwaggerGen(options =>
    {
      options.SwaggerDoc("v1", new OpenApiInfo
      {
        Title = "PocketPilotAI API",
        Version = "v1",
        Description = "Backend for transactions, budgets, imports, and AI coaching insights."
      });

      options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
      {
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT token.",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
      });

      options.AddSecurityRequirement(new OpenApiSecurityRequirement
      {
        {
          new OpenApiSecurityScheme
          {
            Reference = new OpenApiReference
            {
              Type = ReferenceType.SecurityScheme,
              Id = "Bearer"
            }
          },
          Array.Empty<string>()
        }
      });
    });

    return services;
  }
}
