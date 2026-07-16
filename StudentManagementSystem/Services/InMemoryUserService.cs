using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class InMemoryUserService
    {
        private readonly List<ApplicationUser> _users = new();

        public InMemoryUserService()
        {
            // Add a default admin user for testing
            _users.Add(new ApplicationUser
            {
                Id = "admin-001",
                Email = "admin@futuretech.edu",
                FullName = "System Administrator",
                Provider = "Local",
                ProviderKey = "admin-001",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            });
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.Email == email));
        }

        public async Task<ApplicationUser> GetUserByProviderKeyAsync(string provider, string providerKey)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.Provider == provider && u.ProviderKey == providerKey));
        }

        public async Task<ApplicationUser> CreateUserAsync(ApplicationUser user)
        {
            _users.Add(user);
            return await Task.FromResult(user);
        }

        public async Task UpdateUserAsync(ApplicationUser user)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing != null)
            {
                var index = _users.IndexOf(existing);
                _users[index] = user;
            }
            await Task.CompletedTask;
        }

        public async Task<bool> IsAdminAsync(string userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            return await Task.FromResult(user?.Role == "Admin");
        }
    }
}