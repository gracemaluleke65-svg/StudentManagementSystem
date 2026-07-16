using Microsoft.AspNetCore.Http;  // For IFormFile
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels;

public class StudentViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required")]
    [Phone]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    public string EnrolmentStatus { get; set; } = "Active";

    public IFormFile? ProfileImage { get; set; }

    public string? ExistingProfileImageUrl { get; set; }
}