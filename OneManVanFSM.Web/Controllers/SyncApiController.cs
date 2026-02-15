using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

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

    /// <summary>
    /// Track device info from request headers. Updates or creates MobileDevice record.
    /// Call this from any sync endpoint to automatically track device versions.
    /// </summary>
    protected async Task TrackDeviceAsync(AppDbContext db)
    {
        var deviceId = Request.Headers["X-Device-Id"].ToString();
        if (string.IsNullOrWhiteSpace(deviceId)) return;

        var deviceName = Request.Headers["X-Device-Name"].ToString();
        var platform = Request.Headers["X-Platform"].ToString();
        var osVersion = Request.Headers["X-OS-Version"].ToString();
        var appVersion = Request.Headers["X-App-Version"].ToString();
        var buildNumber = Request.Headers["X-Build-Number"].ToString();

        var device = await db.MobileDevices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

        var userId = GetUserId();
        var employeeId = GetEmployeeId();

        if (device is null)
        {
            device = new MobileDevice
            {
                DeviceId = deviceId,
                DeviceName = deviceName,
                Platform = platform ?? "Unknown",
                OsVersion = osVersion,
                AppVersion = appVersion ?? "1.0.0",
                BuildNumber = buildNumber ?? "0",
                UserId = userId > 0 ? userId : null,
                EmployeeId = employeeId,
                FirstSeenAt = DateTime.UtcNow,
                LastSyncTime = DateTime.UtcNow,
                IsActive = true
            };

            // Parse build timestamp if possible
            if (int.TryParse(buildNumber, out var buildNum) && buildNum > 10000)
            {
                try
                {
                    var days = buildNum / 10000;
                    var hourMin = buildNum % 10000;
                    var hour = hourMin / 100;
                    var minute = hourMin % 100;
                    device.BuildTimestamp = new DateTime(2025, 1, 1).AddDays(days).AddHours(hour).AddMinutes(minute);
                }
                catch { /* Invalid format */ }
            }

            db.MobileDevices.Add(device);
        }
        else
        {
            // Update existing device
            device.DeviceName = deviceName ?? device.DeviceName;
            device.Platform = platform ?? device.Platform;
            device.OsVersion = osVersion ?? device.OsVersion;
            device.AppVersion = appVersion ?? device.AppVersion;
            device.BuildNumber = buildNumber ?? device.BuildNumber;
            device.LastSyncTime = DateTime.UtcNow;
            device.UserId = userId > 0 ? userId : device.UserId;
            device.EmployeeId = employeeId ?? device.EmployeeId;
            device.IsActive = true;

            // Update build timestamp
            if (int.TryParse(buildNumber, out var buildNum) && buildNum > 10000)
            {
                try
                {
                    var days = buildNum / 10000;
                    var hourMin = buildNum % 10000;
                    var hour = hourMin / 100;
                    var minute = hourMin % 100;
                    device.BuildTimestamp = new DateTime(2025, 1, 1).AddDays(days).AddHours(hour).AddMinutes(minute);
                }
                catch { /* Invalid format */ }
            }
        }

        await db.SaveChangesAsync();
    }
}
