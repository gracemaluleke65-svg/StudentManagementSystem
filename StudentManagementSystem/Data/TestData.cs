using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.Threading.Tasks;

namespace StudentManagementSystem.Data
{
    public static class TestData
    {
        public static async Task InitializeAsync(InMemoryStudentService studentService)
        {
            // Add some test students
            var testStudents = new[]
            {
                new Student
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@futuretech.edu",
                    MobileNumber = "+1234567890",
                    EnrolmentStatus = "Active"
                },
                new Student
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@futuretech.edu",
                    MobileNumber = "+1987654321",
                    EnrolmentStatus = "Active"
                },
                new Student
                {
                    FirstName = "Mike",
                    LastName = "Johnson",
                    Email = "mike.johnson@futuretech.edu",
                    MobileNumber = "+1555555555",
                    EnrolmentStatus = "Inactive"
                }
            };

            foreach (var student in testStudents)
            {
                if (!await studentService.StudentExistsAsync(student.Email))
                {
                    await studentService.CreateStudentAsync(student);
                }
            }
        }
    }
}