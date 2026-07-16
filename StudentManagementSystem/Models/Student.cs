using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models;

public class Student
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50)]
    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50)]
    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required")]
    [Phone]
    [JsonProperty("mobileNumber")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    [JsonProperty("enrolmentStatus")]
    public string EnrolmentStatus { get; set; } = "Active";

    [JsonProperty("profileImageUrl")]
    public string? ProfileImageUrl { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("isDeleted", DefaultValueHandling = DefaultValueHandling.Include)]
    public bool IsDeleted { get; set; } = false;

    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}";
}