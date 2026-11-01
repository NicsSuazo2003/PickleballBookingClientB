// Services/TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PickleballBookingSystem.Entities;

namespace PickleballBookingSystem.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // ✅ Configurable TTL — defaults to 168 hours (7 days)
        var ttlHours = int.TryParse(_config["Jwt:TtlHours"], out var h) && h > 0 ? h : 168;
        var expires = DateTime.UtcNow.AddHours(ttlHours);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),

            // ✅ NEW — unique token id (revocation-friendly for future use)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            // ✅ NEW — client_id claim (tenant isolation from JWT, not headers)
            new Claim("client_id", user.ClientId.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}