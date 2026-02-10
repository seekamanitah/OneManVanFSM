using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace OneManVanFSM.Web.Services;

public interface IJwtService
{
    string GenerateToken(int userId, string username, string role, int? employeeId);
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly string _issuer = "OneManVanFSM";
    private readonly string _audience = "OneManVanFSM.Mobile";
    private readonly int _expirationHours = 24;

    public JwtService()
    {
        // Use environment variable or generate a stable key from machine name + app name
        _secretKey = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? GenerateStableKey();
    }

    public string GenerateToken(int userId, string username, string role, int? employeeId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new("eid", employeeId?.ToString() ?? ""),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expirationHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out _);
        }
        catch
        {
            return null;
        }
    }

    public string Issuer => _issuer;
    public string Audience => _audience;
    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(_secretKey));

    /// <summary>
    /// Generates a stable 256-bit key derived from the machine name to avoid
    /// invalidating tokens on every restart when JWT_SECRET is not set.
    /// </summary>
    private static string GenerateStableKey()
    {
        var seed = $"OneManVanFSM-{Environment.MachineName}-SyncApi";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToBase64String(hash);
    }
}
