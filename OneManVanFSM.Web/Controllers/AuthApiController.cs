using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models.Api;
using OneManVanFSM.Web.Services;

namespace OneManVanFSM.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public AuthApiController(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Authenticate with user credentials or sync credentials and receive a JWT token.
    /// POST /api/authapi/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiLoginResponse>> Login([FromBody] ApiLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Ok(ApiLoginResponse.Failure("Username and password are required."));

        // Check for dedicated sync account from environment variables
        var syncUser = Environment.GetEnvironmentVariable("SYNC_USER");
        var syncPassword = Environment.GetEnvironmentVariable("SYNC_PASSWORD");

        if (!string.IsNullOrEmpty(syncUser) &&
            request.Username == syncUser &&
            request.Password == syncPassword)
        {
            // Sync account gets Admin-level access
            var token = _jwtService.GenerateToken(0, syncUser, "Admin", null);
            return Ok(ApiLoginResponse.Success(
                token, DateTime.UtcNow.AddHours(24), 0, null, syncUser, "Admin"));
        }

        // Regular user authentication
        var user = await _db.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

        if (user is null)
            return Ok(ApiLoginResponse.Failure("Invalid username or password."));

        if (user.IsLocked)
            return Ok(ApiLoginResponse.Failure("Account is locked. Contact your administrator."));

        if (!user.IsActive)
            return Ok(ApiLoginResponse.Failure("Account is inactive."));

        if (!AuthService.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.LoginAttempts++;
            if (user.LoginAttempts >= 5)
                user.IsLocked = true;
            await _db.SaveChangesAsync();
            return Ok(ApiLoginResponse.Failure("Invalid username or password."));
        }

        // Successful login
        user.LoginAttempts = 0;
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var jwt = _jwtService.GenerateToken(user.Id, user.Username, user.Role.ToString(), user.EmployeeId);
        return Ok(ApiLoginResponse.Success(
            jwt, DateTime.UtcNow.AddHours(24), user.Id, user.EmployeeId, user.Username, user.Role.ToString()));
    }

    /// <summary>
    /// Refresh an existing valid token. Requires valid Bearer token.
    /// POST /api/authapi/refresh
    /// </summary>
    [HttpPost("refresh")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public ActionResult<ApiLoginResponse> Refresh()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var username = User.Identity?.Name ?? "";
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Tech";
        var eidStr = User.FindFirst("eid")?.Value;
        int? eid = int.TryParse(eidStr, out var e) && e > 0 ? e : null;

        var token = _jwtService.GenerateToken(userId, username, role, eid);
        return Ok(ApiLoginResponse.Success(token, DateTime.UtcNow.AddHours(24), userId, eid, username, role));
    }
}
