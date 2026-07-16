using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.Diagnostics;

namespace StudentManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAzureCosmosDbService _cosmosDbService;

    public HomeController(ILogger<HomeController> logger, IAzureCosmosDbService cosmosDbService)
    {
        _logger = logger;
        _cosmosDbService = cosmosDbService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var students = await _cosmosDbService.GetStudentsAsync(1, 1000);
            var studentList = students.ToList();

            ViewBag.StudentCount = studentList.Count;
            ViewBag.ActiveCount = studentList.Count(s => s.EnrolmentStatus == "Active");
            ViewBag.AdminCount = 1; // You can query this from your user service if needed
        }
        catch
        {
            ViewBag.StudentCount = 0;
            ViewBag.ActiveCount = 0;
            ViewBag.AdminCount = 0;
        }

        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}