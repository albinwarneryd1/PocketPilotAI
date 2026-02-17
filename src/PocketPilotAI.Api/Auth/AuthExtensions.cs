using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PocketPilotAI.Api.Auth;

public static class AuthExtensions
{
  public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
  {
    JwtOptions jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    jwtOptions.Issuer = configuration["POCKETPILOTAI_JWT_ISSUER"] ?? jwtOptions.Issuer;
    jwtOptions.Audience = configuration["POCKETPILOTAI_JWT_AUDIENCE"] ?? jwtOptions.Audience;
    jwtOptions.Key = configuration["POCKETPILOTAI_JWT_KEY"] ?? jwtOptions.Key;

    services
      .AddOptions<JwtOptions>()
      .Configure(options =>
      {
        options.Issuer = jwtOptions.Issuer;
        options.Audience = jwtOptions.Audience;
        options.Key = jwtOptions.Key;
        options.ExpirationMinutes = jwtOptions.ExpirationMinutes;
      });

    services.AddSingleton<IJwtTokenService, JwtTokenService>();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidateLifetime = true,
          ValidIssuer = jwtOptions.Issuer,
          ValidAudience = jwtOptions.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
          ClockSkew = TimeSpan.FromMinutes(1)
        };
      });

    services.AddAuthorization();
    return services;
  }
}
