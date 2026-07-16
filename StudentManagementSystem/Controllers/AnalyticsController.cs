using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Services;
using System.Globalization;

namespace StudentManagementSystem.Controllers;

[Authorize]
public class AnalyticsController : Controller
{
    private readonly IAzureCosmosDbService _cosmosDbService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAzureCosmosDbService cosmosDbService, ILogger<AnalyticsController> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Get all students for analysis
            var allStudents = await _cosmosDbService.GetStudentsAsync(1, 1000);
            var studentsList = allStudents.ToList();

            // Basic counts
            ViewBag.Total = studentsList.Count;
            ViewBag.Active = studentsList.Count(s => s.EnrolmentStatus == "Active");
            ViewBag.Inactive = studentsList.Count(s => s.EnrolmentStatus == "Inactive");

            // Calculate percentages
            ViewBag.ActivePercentage = studentsList.Any() ?
                Math.Round((double)ViewBag.Active / ViewBag.Total * 100, 1) : 0;
            ViewBag.InactivePercentage = studentsList.Any() ?
                Math.Round((double)ViewBag.Inactive / ViewBag.Total * 100, 1) : 0;

            // Monthly registration data (last 6 months)
            var months = new List<string>();
            var counts = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var monthName = date.ToString("MMM yyyy");
                months.Add(monthName);

                // Count students created in this month (simulated based on CreatedAt)
                var count = studentsList.Count(s =>
                    s.CreatedAt.Month == date.Month && s.CreatedAt.Year == date.Year);
                counts.Add(count);
            }

            ViewBag.Months = months;
            ViewBag.Counts = counts;

            // Recent activity (last 5 students added)
            ViewBag.RecentStudents = studentsList
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToList();

            // Domain analysis (group by email domain)
            var domainGroups = studentsList
                .GroupBy(s => s.Email.Split('@').LastOrDefault() ?? "Unknown")
                .Select(g => new { Domain = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            ViewBag.TopDomains = domainGroups.Select(x => x.Domain).ToList();
            ViewBag.DomainCounts = domainGroups.Select(x => x.Count).ToList();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading analytics");
            TempData["Error"] = "Failed to load analytics data.";
            return View();
        }
    }
}