using Microsoft.AspNetCore.Mvc;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Web.Services;

namespace OneManVanFSM.Web.Controllers;

[Route("api/shift")]
public class ShiftApiController : SyncApiController
{
    private readonly AppDbContext _db;
    private readonly IShiftService _shiftService;

    public ShiftApiController(AppDbContext db, IShiftService shiftService)
    {
        _db = db;
        _shiftService = shiftService;
    }

    /// <summary>GET /api/shift/teamboard</summary>
    [HttpGet("teamboard")]
    public async Task<ActionResult<List<EmployeeShiftStatus>>> GetTeamBoard()
    {
        await TrackDeviceAsync(_db);
        var board = await _shiftService.GetTeamBoardAsync();
        return Ok(board);
    }

    /// <summary>POST /api/shift/clockin</summary>
    [HttpPost("clockin")]
    public async Task<IActionResult> ClockIn([FromBody] ShiftClockRequest req)
    {
        try
        {
            await _shiftService.ClockInEmployeeAsync(req.EmployeeId, req.Notes);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>POST /api/shift/clockout</summary>
    [HttpPost("clockout")]
    public async Task<IActionResult> ClockOut([FromBody] ShiftClockRequest req)
    {
        try
        {
            await _shiftService.ClockOutEmployeeAsync(req.EmployeeId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>POST /api/shift/assign</summary>
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] ShiftAssignRequest req)
    {
        try
        {
            await _shiftService.AssignEmployeeToJobAsync(req.EmployeeId, req.JobId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>POST /api/shift/unassign</summary>
    [HttpPost("unassign")]
    public async Task<IActionResult> Unassign([FromBody] ShiftClockRequest req)
    {
        try
        {
            await _shiftService.UnassignEmployeeFromJobAsync(req.EmployeeId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>GET /api/shift/jobs</summary>
    [HttpGet("jobs")]
    public async Task<ActionResult<List<ShiftJobOption>>> GetAssignableJobs()
    {
        var jobs = await _shiftService.GetAssignableJobsAsync();
        return Ok(jobs);
    }
}

public class ShiftClockRequest
{
    public int EmployeeId { get; set; }
    public string? Notes { get; set; }
}

public class ShiftAssignRequest
{
    public int EmployeeId { get; set; }
    public int JobId { get; set; }
}
