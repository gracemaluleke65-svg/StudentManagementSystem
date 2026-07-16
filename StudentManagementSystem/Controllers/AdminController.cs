using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly IAzureCosmosDbService _cosmosDbService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAzureCosmosDbService cosmosDbService, ILogger<AdminController> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var dashboard = new AdminDashboardViewModel();

            // Get all students
            var allStudents = await _cosmosDbService.GetAllStudentsForAnalyticsAsync();
            dashboard.TotalStudents = allStudents.Count();
            dashboard.ActiveStudents = allStudents.Count(s => s.EnrolmentStatus == "Active" && !s.IsDeleted);
            dashboard.InactiveStudents = allStudents.Count(s => s.EnrolmentStatus == "Inactive" && !s.IsDeleted);
            dashboard.ArchivedStudents = allStudents.Count(s => s.IsDeleted);

            // Calculate percentages
            dashboard.ActivePercentage = dashboard.TotalStudents > 0
                ? Math.Round((double)dashboard.ActiveStudents / dashboard.TotalStudents * 100, 1)
                : 0;
            dashboard.InactivePercentage = dashboard.TotalStudents > 0
                ? Math.Round((double)dashboard.InactiveStudents / dashboard.TotalStudents * 100, 1)
                : 0;
            dashboard.ArchivedPercentage = dashboard.TotalStudents > 0
                ? Math.Round((double)dashboard.ArchivedStudents / dashboard.TotalStudents * 100, 1)
                : 0;

            // Get users count
            var users = await _cosmosDbService.GetAllUsersAsync();
            dashboard.TotalUsers = users.Count();
            dashboard.AdminUsers = users.Count(u => u.Role == "Admin");

            // Monthly trends (last 6 months)
            var months = new List<string>();
            var registrations = new List<int>();
            var activeTrend = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var monthName = date.ToString("MMM yyyy");
                months.Add(monthName);

                var monthStart = new DateTime(date.Year, date.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var regCount = allStudents.Count(s => s.CreatedAt >= monthStart && s.CreatedAt < monthEnd);
                registrations.Add(regCount);

                var activeCount = allStudents.Count(s => s.EnrolmentStatus == "Active" && !s.IsDeleted &&
                    s.UpdatedAt >= monthStart && s.UpdatedAt < monthEnd);
                activeTrend.Add(activeCount);
            }

            dashboard.Months = months;
            dashboard.RegistrationTrend = registrations;
            dashboard.ActiveTrend = activeTrend;

            // Status distribution
            dashboard.StatusLabels = new List<string> { "Active", "Inactive", "Archived" };
            dashboard.StatusData = new List<int> { dashboard.ActiveStudents, dashboard.InactiveStudents, dashboard.ArchivedStudents };
            dashboard.StatusColors = new List<string> { "#10b981", "#94a3b8", "#f59e0b" };

            // Recent activities (last 5 students added)
            dashboard.RecentStudents = allStudents
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToList();

            // Recent archived (last 5 archived students)
            dashboard.RecentArchived = allStudents
                .Where(s => s.IsDeleted)
                .OrderByDescending(s => s.UpdatedAt)
                .Take(5)
                .ToList();

            // Storage metrics
            dashboard.CosmosDbStatus = "Connected";
            dashboard.BlobStorageStatus = "Connected";
            dashboard.LastSyncTime = DateTime.UtcNow.AddHours(2); // SAST

            return View(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            TempData["Error"] = "Failed to load dashboard data.";
            return View(new AdminDashboardViewModel());
        }
    }
}