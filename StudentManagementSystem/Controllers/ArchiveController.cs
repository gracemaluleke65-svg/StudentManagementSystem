using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers;

[Authorize]
public class ArchiveController : Controller
{
    private readonly IAzureCosmosDbService _cosmosDbService;
    private readonly ILogger<ArchiveController> _logger;

    public ArchiveController(IAzureCosmosDbService cosmosDbService, ILogger<ArchiveController> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;

        var archivedStudents = await _cosmosDbService.GetArchivedStudentsAsync(page, pageSize);
        var totalCount = await _cosmosDbService.GetArchivedStudentCountAsync();

        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return View(archivedStudents);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        try
        {
            var student = await _cosmosDbService.GetArchivedStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            await _cosmosDbService.RestoreStudentAsync(id);
            TempData["Success"] = $"Student {student.FullName} has been restored successfully.";
            _logger.LogInformation("Student {StudentId} restored from archive", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring student {StudentId}", id);
            TempData["Error"] = "An error occurred while restoring the student.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDelete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        try
        {
            var student = await _cosmosDbService.GetArchivedStudentAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            await _cosmosDbService.DeleteStudentAsync(id, permanent: true);
            TempData["Success"] = $"Student {student.FullName} has been permanently deleted.";
            _logger.LogInformation("Student {StudentId} permanently deleted from archive", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting student {StudentId}", id);
            TempData["Error"] = "An error occurred while permanently deleting the student.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmptyRecycleBin()
    {
        try
        {
            var count = await _cosmosDbService.EmptyRecycleBinAsync();
            TempData["Success"] = $"Successfully deleted {count} archived students permanently.";
            _logger.LogInformation("Empty recycle bin completed, deleted {Count} students", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emptying recycle bin");
            TempData["Error"] = "An error occurred while emptying the recycle bin.";
        }

        return RedirectToAction(nameof(Index));
    }
}