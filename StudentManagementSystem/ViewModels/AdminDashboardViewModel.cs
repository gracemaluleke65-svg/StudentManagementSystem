using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels;

public class AdminDashboardViewModel
{
    // Student Metrics
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int InactiveStudents { get; set; }
    public int ArchivedStudents { get; set; }

    public double ActivePercentage { get; set; }
    public double InactivePercentage { get; set; }
    public double ArchivedPercentage { get; set; }

    // User Metrics
    public int TotalUsers { get; set; }
    public int AdminUsers { get; set; }

    // Chart Data
    public List<string> Months { get; set; } = new();
    public List<int> RegistrationTrend { get; set; } = new();
    public List<int> ActiveTrend { get; set; } = new();

    // Status Distribution
    public List<string> StatusLabels { get; set; } = new();
    public List<int> StatusData { get; set; } = new();
    public List<string> StatusColors { get; set; } = new();

    // Recent Activities
    public List<Student> RecentStudents { get; set; } = new();
    public List<Student> RecentArchived { get; set; } = new();

    // System Metrics
    public string CosmosDbStatus { get; set; } = "Checking...";
    public string BlobStorageStatus { get; set; } = "Checking...";
    public DateTime LastSyncTime { get; set; }
}