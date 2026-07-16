using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class InMemoryStudentService
    {
        private readonly List<Student> _students = new();
        private int _nextId = 1;

        public async Task<IEnumerable<Student>> GetStudentsAsync()
        {
            return await Task.FromResult(_students.Where(s => !s.IsDeleted).ToList());
        }

        public async Task<Student> GetStudentAsync(string id)
        {
            return await Task.FromResult(_students.FirstOrDefault(s => s.Id == id && !s.IsDeleted));
        }

        public async Task<bool> StudentExistsAsync(string email)
        {
            return await Task.FromResult(_students.Any(s => s.Email == email && !s.IsDeleted));
        }

        public async Task<Student> CreateStudentAsync(Student student)
        {
            student.Id = (_nextId++).ToString();
            student.CreatedAt = DateTime.UtcNow;
            student.UpdatedAt = DateTime.UtcNow;
            _students.Add(student);
            return await Task.FromResult(student);
        }

        public async Task<Student> UpdateStudentAsync(string id, Student student)
        {
            var existing = _students.FirstOrDefault(s => s.Id == id);
            if (existing != null)
            {
                var index = _students.IndexOf(existing);
                student.Id = id; // Keep the same ID
                student.CreatedAt = existing.CreatedAt;
                student.UpdatedAt = DateTime.UtcNow;
                _students[index] = student;
            }
            return await Task.FromResult(student);
        }

        public async Task DeleteStudentAsync(string id)
        {
            var student = _students.FirstOrDefault(s => s.Id == id);
            if (student != null)
            {
                student.IsDeleted = true;
                student.UpdatedAt = DateTime.UtcNow;
            }
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            var results = _students.Where(s =>
                !s.IsDeleted && (
                    s.FirstName.ToLower().Contains(searchTerm) ||
                    s.LastName.ToLower().Contains(searchTerm) ||
                    s.Id.Contains(searchTerm)
                )
            ).ToList();

            return await Task.FromResult(results);
        }
    }
}