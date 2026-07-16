using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services;

public interface IAzureCosmosDbService
{
    // Student CRUD
    Task<Student?> GetStudentAsync(string id);
    Task<IEnumerable<Student>> GetStudentsAsync(int page = 1, int pageSize = 10);
    Task<int> GetTotalStudentCountAsync();
    Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm, int page = 1, int pageSize = 10);
    Task<Student> CreateStudentAsync(Student student);
    Task<Student> UpdateStudentAsync(string id, Student student);
    Task DeleteStudentAsync(string id, bool permanent = false);
    Task<bool> StudentExistsAsync(string email);

    // Archive/Recycle Bin Methods
    Task<IEnumerable<Student>> GetArchivedStudentsAsync(int page = 1, int pageSize = 10);
    Task<int> GetArchivedStudentCountAsync();
    Task<Student?> GetArchivedStudentAsync(string id);
    Task RestoreStudentAsync(string id);
    Task<int> EmptyRecycleBinAsync();

    // Analytics Methods
    Task<IEnumerable<Student>> GetAllStudentsForAnalyticsAsync();
    Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

    // User CRUD
    Task<ApplicationUser?> GetUserByProviderAsync(string provider, string providerKey);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<ApplicationUser> CreateUserAsync(ApplicationUser user);
    Task UpdateUserAsync(ApplicationUser user);
}