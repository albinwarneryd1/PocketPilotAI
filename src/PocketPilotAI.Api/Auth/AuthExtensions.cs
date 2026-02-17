using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PocketPilotAI.Api.Auth;

public static class AuthExtensions
{
  public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddOptions<JwtOptions>()
      .Bind(configuration.GetSection(JwtOptions.SectionName))
      .PostConfigure(options =>
      {
        options.Issuer = configuration["POCKETPILOTAI_JWT_ISSUER"] ?? options.Issuer;
        options.Audience = configuration["POCKETPILOTAI_JWT_AUDIENCE"] ?? options.Audience;
        options.Key = configuration["POCKETPILOTAI_JWT_KEY"] ?? options.Key;
      });

    services.AddSingleton<IJwtTokenService, JwtTokenService>();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer((options, provider) =>
      {
        JwtOptions jwt = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidateLifetime = true,
          ValidIssuer = jwt.Issuer,
          ValidAudience = jwt.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
          ClockSkew = TimeSpan.FromMinutes(1)
        };
      });

    services.AddAuthorization();
    return services;
  }
}
