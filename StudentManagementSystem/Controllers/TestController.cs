using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers
{
    public class TestController : Controller
    {
        private readonly InMemoryUserService _userService;

        public TestController(InMemoryUserService userService)
        {
            _userService = userService;
        }

        // Quick test login - remove this after OAuth is working
        public async Task<IActionResult> QuickLogin()
        {
            var user = await _userService.GetUserByEmailAsync("admin@futuretech.edu");

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserId", user.Id),
                    new Claim("Role", user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Student");
            }

            return RedirectToAction("Login", "Account");
        }
    }
}