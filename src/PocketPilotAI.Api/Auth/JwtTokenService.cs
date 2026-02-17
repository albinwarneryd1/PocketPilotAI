using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PocketPilotAI.Core.Application.Dtos.Users;

namespace PocketPilotAI.Api.Auth;

public interface IJwtTokenService
{
  (string token, DateTime expiresUtc) CreateToken(UserDto user);
}

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
  public (string token, DateTime expiresUtc) CreateToken(UserDto user)
  {
    JwtOptions jwt = options.Value;

    DateTime expires = DateTime.UtcNow.AddMinutes(jwt.ExpirationMinutes);
    List<Claim> claims =
    [
      new(ClaimTypes.NameIdentifier, user.Id.ToString()),
      new(ClaimTypes.Name, user.DisplayName),
      new(ClaimTypes.Email, user.Email)
    ];

    SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwt.Key));
    SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

    JwtSecurityToken token = new(
      issuer: jwt.Issuer,
      audience: jwt.Audience,
      claims: claims,
      expires: expires,
      signingCredentials: credentials);

    return (new JwtSecurityTokenHandler().WriteToken(token), expires);
  }
}
