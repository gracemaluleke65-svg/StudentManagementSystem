using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;  // ADDED: For IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers;

[Authorize]
public class StudentController : Controller
{
    private readonly IAzureCosmosDbService _cosmosDbService;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        IAzureCosmosDbService cosmosDbService,
        IAzureBlobStorageService blobStorageService,
        ILogger<StudentController> logger)
    {
        _cosmosDbService = cosmosDbService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;

        var totalCount = await _cosmosDbService.GetTotalStudentCountAsync();
        ViewBag.TotalCount = totalCount;

        _logger.LogInformation("Index action: TotalCount={TotalCount}, Page={Page}, PageSize={PageSize}", totalCount, page, pageSize);

        var students = await _cosmosDbService.GetStudentsAsync(page, pageSize);

        _logger.LogInformation("Index action: Retrieved {StudentCount} students for display", students.Count());

        // Debug: Check if students is null or empty
        if (students == null)
        {
            _logger.LogWarning("Index action: students is NULL");
        }
        else if (!students.Any())
        {
            _logger.LogWarning("Index action: students is EMPTY");
        }

        return View(students);
    }

    public IActionResult Create() => View(new StudentViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            model.Email = model.Email.Trim().ToLowerInvariant();

            if (await _cosmosDbService.StudentExistsAsync(model.Email))
            {
                ModelState.AddModelError("Email", "A student with this email already exists.");
                return View(model);
            }

            string? imageUrl = null;
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    ModelState.AddModelError("ProfileImage", "Only JPEG and PNG files are allowed.");
                    return View(model);
                }

                using var stream = model.ProfileImage.OpenReadStream();
                if (!await _blobStorageService.ValidateImageAsync(stream))
                {
                    ModelState.AddModelError("ProfileImage", "Invalid image format or size exceeds 5MB.");
                    return View(model);
                }

                stream.Position = 0;

                var fileName = $"student-{Guid.NewGuid():N}.jpg";
                imageUrl = await _blobStorageService.UploadProfileImageAsync(stream, fileName, "image/jpeg");
            }

            var student = new Student
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = model.Email,
                MobileNumber = model.MobileNumber.Trim(),
                EnrolmentStatus = model.EnrolmentStatus,
                ProfileImageUrl = imageUrl
            };

            await _cosmosDbService.CreateStudentAsync(student);
            TempData["Success"] = $"Student {student.FullName} created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            ModelState.AddModelError("", "An error occurred while creating the student.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var student = await _cosmosDbService.GetStudentAsync(id);
        if (student == null) return NotFound();

        var model = new StudentViewModel
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            MobileNumber = student.MobileNumber,
            EnrolmentStatus = student.EnrolmentStatus,
            ExistingProfileImageUrl = student.ProfileImageUrl
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, StudentViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        try
        {
            var existing = await _cosmosDbService.GetStudentAsync(id);
            if (existing == null) return NotFound();

            model.Email = model.Email.Trim().ToLowerInvariant();

            if (existing.Email != model.Email && await _cosmosDbService.StudentExistsAsync(model.Email))
            {
                ModelState.AddModelError("Email", "A student with this email already exists.");
                return View(model);
            }

            string? imageUrl = existing.ProfileImageUrl;

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var ext = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    ModelState.AddModelError("ProfileImage", "Only JPEG and PNG files are allowed.");
                    return View(model);
                }

                using var stream = model.ProfileImage.OpenReadStream();
                if (!await _blobStorageService.ValidateImageAsync(stream))
                {
                    ModelState.AddModelError("ProfileImage", "Invalid image format or size exceeds 5MB.");
                    return View(model);
                }

                stream.Position = 0;

                if (!string.IsNullOrEmpty(existing.ProfileImageUrl))
                {
                    var oldBlobName = _blobStorageService.GetBlobNameFromUrl(existing.ProfileImageUrl);
                    if (!string.IsNullOrEmpty(oldBlobName))
                        await _blobStorageService.DeleteProfileImageAsync(oldBlobName);
                }

                var fileName = $"student-{Guid.NewGuid():N}.jpg";
                imageUrl = await _blobStorageService.UploadProfileImageAsync(stream, fileName, "image/jpeg");
            }

            existing.FirstName = model.FirstName.Trim();
            existing.LastName = model.LastName.Trim();
            existing.Email = model.Email;
            existing.MobileNumber = model.MobileNumber.Trim();
            existing.EnrolmentStatus = model.EnrolmentStatus;
            existing.ProfileImageUrl = imageUrl;

            await _cosmosDbService.UpdateStudentAsync(id, existing);
            TempData["Success"] = $"Student {existing.FullName} updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student {StudentId}", id);
            ModelState.AddModelError("", "An error occurred while updating the student.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest();

        try
        {
            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null) return NotFound();

            await _cosmosDbService.DeleteStudentAsync(id, permanent: false);
            TempData["Success"] = $"Student {student.FullName} has been deactivated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft-deleting student {StudentId}", id);
            TempData["Error"] = "An error occurred while deactivating the student.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDelete(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest();

        try
        {
            var student = await _cosmosDbService.GetStudentAsync(id);
            if (student == null) return NotFound();

            if (!string.IsNullOrEmpty(student.ProfileImageUrl))
            {
                var blobName = _blobStorageService.GetBlobNameFromUrl(student.ProfileImageUrl);
                if (!string.IsNullOrEmpty(blobName))
                    await _blobStorageService.DeleteProfileImageAsync(blobName);
            }

            await _cosmosDbService.DeleteStudentAsync(id, permanent: true);
            TempData["Success"] = $"Student {student.FullName} has been permanently deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting student {StudentId}", id);
            TempData["Error"] = "An error occurred while permanently deleting the student.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Search(string searchTerm, int page = 1, int pageSize = 10)
    {
        searchTerm = searchTerm?.Trim() ?? "";

        if (searchTerm.Length > 100)
            searchTerm = searchTerm.Substring(0, 100);

        var students = await _cosmosDbService.SearchStudentsAsync(searchTerm, page, pageSize);
        return PartialView("_StudentList", students);
    }
}