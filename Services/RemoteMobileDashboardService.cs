using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode dashboard service. Fetches aggregated dashboard data from the
/// server API. Falls back to a minimal local dashboard on network failure.
/// </summary>
public class RemoteMobileDashboardService : IMobileDashboardService
{
    private readonly ApiClient _api;
    private readonly ILogger<RemoteMobileDashboardService> _logger;

    public RemoteMobileDashboardService(ApiClient api, ILogger<RemoteMobileDashboardService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<MobileDashboardData> GetDashboardAsync(int employeeId, bool isElevated = false)
    {
        try
        {
            var url = isElevated
                ? $"api/dashboard/{employeeId}?elevated=true"
                : $"api/dashboard/{employeeId}";
            var response = await _api.GetAsync<RemoteDashboardDto>(url);
            if (response is null)
                return EmptyDashboard();

            return new MobileDashboardData
            {
                TodayJobCount = response.TodayJobCount,
                OpenJobCount = response.OpenJobCount,
                PendingNoteCount = response.PendingNoteCount,
                HoursToday = response.HoursToday,
                HoursThisWeek = response.HoursThisWeek,
                IsClockedIn = response.IsClockedIn,
                ClockInTime = response.ClockInTime,
                CompletedThisWeek = response.CompletedThisWeek,
                OverdueJobCount = response.OverdueJobCount,
                UpcomingJobCount = response.UpcomingJobCount,
                LowStockCount = response.LowStockCount,
                ExpiringAgreementCount = response.ExpiringAgreementCount,
                MaintenanceDueCount = response.MaintenanceDueCount,
                WarrantyAlertCount = response.WarrantyAlertCount,
                ActiveJobClockCount = response.ActiveJobClockCount,
                ActiveJobName = response.ActiveJobName,
                JobHoursToday = response.JobHoursToday,
                DraftEstimateCount = response.DraftEstimateCount,
                PendingInvoiceCount = response.PendingInvoiceCount,
                TodayJobs = response.TodayJobs?.Select(j => new MobileJobCard
                {
                    Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                    CustomerName = j.CustomerName, CompanyName = j.CompanyName,
                    SiteAddress = j.SiteAddress, Status = j.Status, Priority = j.Priority,
                    ScheduledDate = j.ScheduledDate, ScheduledTime = j.ScheduledTime,
                    EstimatedDuration = j.EstimatedDuration
                }).ToList() ?? [],
                UpcomingJobs = response.UpcomingJobs?.Select(j => new MobileJobCard
                {
                    Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                    CustomerName = j.CustomerName, CompanyName = j.CompanyName,
                    SiteAddress = j.SiteAddress, Status = j.Status, Priority = j.Priority,
                    ScheduledDate = j.ScheduledDate, ScheduledTime = j.ScheduledTime,
                    EstimatedDuration = j.EstimatedDuration
                }).ToList() ?? [],
                RecentActivity = response.RecentActivity?.Select(a => new MobileActivityItem
                {
                    Description = a.Description, Icon = a.Icon, Timestamp = a.Timestamp
                }).ToList() ?? []
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard API call failed, returning empty dashboard.");
            return EmptyDashboard();
        }
    }

    private static MobileDashboardData EmptyDashboard() => new()
    {
        TodayJobs = [], UpcomingJobs = [], RecentActivity = []
    };

    // DTO that matches the server-side MobileDashboardResponse shape
    private class RemoteDashboardDto
    {
        public int TodayJobCount { get; set; }
        public int OpenJobCount { get; set; }
        public int PendingNoteCount { get; set; }
        public decimal HoursToday { get; set; }
        public decimal HoursThisWeek { get; set; }
        public bool IsClockedIn { get; set; }
        public DateTime? ClockInTime { get; set; }
        public int CompletedThisWeek { get; set; }
        public int OverdueJobCount { get; set; }
        public int UpcomingJobCount { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringAgreementCount { get; set; }
        public int MaintenanceDueCount { get; set; }
        public int WarrantyAlertCount { get; set; }
        public int ActiveJobClockCount { get; set; }
        public string? ActiveJobName { get; set; }
        public decimal JobHoursToday { get; set; }
        public int DraftEstimateCount { get; set; }
        public int PendingInvoiceCount { get; set; }
        public List<RemoteJobCardDto>? TodayJobs { get; set; }
        public List<RemoteJobCardDto>? UpcomingJobs { get; set; }
        public List<RemoteActivityDto>? RecentActivity { get; set; }
    }

    private class RemoteJobCardDto
    {
        public int Id { get; set; }
        public string JobNumber { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? CustomerName { get; set; }
        public string? CompanyName { get; set; }
        public string? SiteAddress { get; set; }
        public JobStatus Status { get; set; }
        public JobPriority Priority { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public TimeSpan? ScheduledTime { get; set; }
        public decimal? EstimatedDuration { get; set; }
    }

    private class RemoteActivityDto
    {
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-circle";
        public DateTime Timestamp { get; set; }
    }
}
