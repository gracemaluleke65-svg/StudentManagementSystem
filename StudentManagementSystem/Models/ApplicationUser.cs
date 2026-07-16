using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models;

public class ApplicationUser
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [JsonProperty("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonProperty("profilePicture")]
    public string? ProfilePicture { get; set; }

    [JsonProperty("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonProperty("providerKey")]
    public string ProviderKey { get; set; } = string.Empty;

    [JsonProperty("role")]
    public string Role { get; set; } = "Admin";

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("lastLogin")]
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;

    [JsonProperty("isActive", DefaultValueHandling = DefaultValueHandling.Include)]
    public bool IsActive { get; set; } = true;
}