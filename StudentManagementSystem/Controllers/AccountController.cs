using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.Security.Claims;

namespace StudentManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly IAzureCosmosDbService _cosmosDbService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAzureCosmosDbService cosmosDbService,
        IConfiguration configuration,
        ILogger<AccountController> logger)
    {
        _cosmosDbService = cosmosDbService;
        _configuration = configuration;
        _logger = logger;
    }

    public IActionResult Login()
    {
        // If already logged in, redirect to appropriate page
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            return RedirectToAction("Index", "Home");
        }

        ViewBag.GoogleClientId = _configuration["Authentication:Google:ClientId"];
        ViewBag.GitHubClientId = _configuration["Authentication:GitHub:ClientId"];
        return View();
    }

    public IActionResult AccessDenied() => View();

    // Admin Login with Email/Password
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            TempData["Error"] = "Please enter both email and password.";
            return RedirectToAction(nameof(Login));
        }

        try
        {
            // Demo admin account
            if (email.ToLower() == "admin@futuretech.edu" && password == "Admin@123")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, "admin-001"),
                    new Claim(ClaimTypes.Name, "System Administrator"),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("UserId", "admin-001"),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                        AllowRefresh = true
                    });

                _logger.LogInformation("Admin logged in: {Email}", email);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                // Redirect admin to Admin Dashboard
                return RedirectToAction("Dashboard", "Admin");
            }

            // Check if user exists in Cosmos DB
            var user = await _cosmosDbService.GetUserByEmailAsync(email);
            if (user != null && user.Role == "Admin")
            {
                if (password == "Admin@123") // Replace with proper password verification
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("UserId", user.Id),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                            AllowRefresh = true
                        });

                    _logger.LogInformation("User logged in: {Email}", email);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return LocalRedirect(returnUrl);

                    // Redirect admin to Admin Dashboard
                    return RedirectToAction("Dashboard", "Admin");
                }
            }

            TempData["Error"] = "Invalid email or password.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin login for {Email}", email);
            TempData["Error"] = "An error occurred during login. Please try again.";
            return RedirectToAction(nameof(Login));
        }
    }

    [HttpGet]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(provider))
        {
            TempData["Error"] = "Authentication provider not specified.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        _logger.LogInformation("Initiating external login with provider: {Provider}", provider);

        return Challenge(properties, provider);
    }

    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrEmpty(remoteError))
        {
            _logger.LogWarning("External login error from provider: {Error}", remoteError);
            TempData["Error"] = $"Authentication failed: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal?.Identity == null)
        {
            _logger.LogWarning("Authentication result failed or principal is null");
            TempData["Error"] = "Authentication failed. Please try again.";
            return RedirectToAction(nameof(Login));
        }

        var email = authenticateResult.Principal.FindFirst(ClaimTypes.Email)?.Value
                   ?? authenticateResult.Principal.FindFirst("email")?.Value;
        var name = authenticateResult.Principal.FindFirst(ClaimTypes.Name)?.Value
                  ?? authenticateResult.Principal.FindFirst("name")?.Value;
        var providerKey = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var provider = authenticateResult.Properties?.Items[".AuthScheme"]
                      ?? authenticateResult.Properties?.Items["LoginProvider"]
                      ?? "Unknown";

        _logger.LogInformation("OAuth callback received - Provider: {Provider}, Email: {Email}, Name: {Name}",
            provider, email, name);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
        {
            _logger.LogWarning("Missing required claims");
            TempData["Error"] = "Unable to retrieve required information from authentication provider.";
            return RedirectToAction(nameof(Login));
        }

        try
        {
            var user = await _cosmosDbService.GetUserByProviderAsync(provider, providerKey);

            if (user == null)
            {
                var existingUser = await _cosmosDbService.GetUserByEmailAsync(email);
                if (existingUser != null)
                {
                    _logger.LogWarning("User {Email} already exists with different provider", email);
                    TempData["Error"] = "An account with this email already exists using a different login method.";
                    return RedirectToAction(nameof(Login));
                }

                // FIXED: Create new user with "User" role, NOT "Admin"
                user = new ApplicationUser
                {
                    Email = email,
                    FullName = name ?? email.Split('@')[0],
                    Provider = provider,
                    ProviderKey = providerKey,
                    ProfilePicture = GetProfilePictureUrl(authenticateResult.Principal, provider),
                    Role = "User"  // Changed from "Admin" to "User"
                };

                user = await _cosmosDbService.CreateUserAsync(user);
                _logger.LogInformation("Created new user: {Email} via {Provider}", email, provider);
            }
            else
            {
                user.LastLogin = DateTime.UtcNow;
                user.ProfilePicture = GetProfilePictureUrl(authenticateResult.Principal, provider) ?? user.ProfilePicture;
                await _cosmosDbService.UpdateUserAsync(user);
                _logger.LogInformation("User logged in: {Email}", email);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.Id),
                new Claim(ClaimTypes.Role, user.Role)  // This will be "User" for Google logins
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            // FIXED: Redirect to Home page for regular users (not Admin Dashboard)
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing external login for {Email}", email);
            TempData["Error"] = "An error occurred during login. Please try again.";
            return RedirectToAction(nameof(Login));
        }
    }

    private string? GetProfilePictureUrl(ClaimsPrincipal principal, string provider)
    {
        return provider switch
        {
            "Google" => principal.FindFirst("picture")?.Value,
            "GitHub" => principal.FindFirst("avatar_url")?.Value
                       ?? principal.FindFirst("urn:github:avatar")?.Value,
            _ => null
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}