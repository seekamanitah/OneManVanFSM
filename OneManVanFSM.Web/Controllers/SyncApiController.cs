using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OneManVanFSM.Web.Controllers;

/// <summary>
/// Base controller for all sync API endpoints.
/// Requires JWT Bearer authentication via the "Api" scheme.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public abstract class SyncApiController : ControllerBase
{
    protected int GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null ? int.Parse(claim.Value) : 0;
    }

    protected int? GetEmployeeId()
    {
        var claim = User.FindFirst("eid");
        if (claim is not null && int.TryParse(claim.Value, out var eid) && eid > 0)
            return eid;
        return null;
    }

    protected string GetUserRole()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Tech";
    }
}
