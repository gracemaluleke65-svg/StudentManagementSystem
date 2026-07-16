using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Configure Azure Cosmos DB Service
builder.Services.AddSingleton<IAzureCosmosDbService, AzureCosmosDbService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<AzureCosmosDbService>>();
    var service = new AzureCosmosDbService(config, logger);
    service.InitializeAsync().GetAwaiter().GetResult();
    return service;
});

// Configure Azure Blob Storage Service
builder.Services.AddSingleton<AzureBlobStorageService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
    var service = new AzureBlobStorageService(config, logger);
    service.InitializeAsync().GetAwaiter().GetResult();
    return service;
});

builder.Services.AddSingleton<IAzureBlobStorageService>(sp => sp.GetRequiredService<AzureBlobStorageService>());

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Events.OnRemoteFailure = context =>
    {
        context.Response.Redirect("/Account/Login?error=" + Uri.EscapeDataString(context.Failure?.Message ?? "Unknown error"));
        context.HandleResponse();
        return Task.CompletedTask;
    };

    options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
    options.ClaimActions.MapJsonKey("urn:google:name", "name", "string");
});

// GitHub (optional)
var gitHubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var gitHubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];

if (!string.IsNullOrEmpty(gitHubClientId) && !gitHubClientId.StartsWith("YOUR_") &&
    !string.IsNullOrEmpty(gitHubClientSecret) && !gitHubClientSecret.StartsWith("YOUR_"))
{
    builder.Services.AddAuthentication()
        .AddGitHub(options =>
        {
            options.ClientId = gitHubClientId;
            options.ClientSecret = gitHubClientSecret;
            options.CallbackPath = "/signin-github";
            options.SaveTokens = true;
            options.Scope.Add("read:user");
            options.Scope.Add("user:email");
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "Admin"));
});

builder.Services.AddLogging();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add cache-control headers
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Custom redirect for admin users after login
app.Use(async (context, next) =>
{
    await next();

    // If user is authenticated and trying to access Home/Index, redirect to Admin/Dashboard if they are admin
    if (context.User.Identity?.IsAuthenticated == true &&
        context.Request.Path == "/" || context.Request.Path == "/Home/Index")
    {
        var isAdmin = context.User.IsInRole("Admin");
        if (isAdmin)
        {
            context.Response.Redirect("/Admin/Dashboard");
        }
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await SeedSampleDataAsync(app.Services);

app.Run();

async Task SeedSampleDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var cosmosDb = scope.ServiceProvider.GetRequiredService<IAzureCosmosDbService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var count = await cosmosDb.GetTotalStudentCountAsync();
        if (count == 0)
        {
            var sample = new List<Student>
            {
                new() {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@futuretech.edu",
                    MobileNumber = "+1234567890",
                    EnrolmentStatus = "Active"
                },
                new() {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@futuretech.edu",
                    MobileNumber = "+1987654321",
                    EnrolmentStatus = "Active"
                },
                new() {
                    FirstName = "Mike",
                    LastName = "Johnson",
                    Email = "mike.johnson@futuretech.edu",
                    MobileNumber = "+1555555555",
                    EnrolmentStatus = "Inactive"
                }
            };

            foreach (var s in sample)
            {
                await cosmosDb.CreateStudentAsync(s);
            }

            logger.LogInformation("Seeded {Count} sample students", sample.Count);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed sample data");
    }
}