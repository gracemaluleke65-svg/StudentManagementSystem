using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using StudentManagementSystem.Controllers;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.Security.Claims;
using Xunit;

namespace StudentManagementSystem.Tests.Controllers;

public class AnalyticsControllerTests
{
    private readonly Mock<IAzureCosmosDbService> _cosmosMock;
    private readonly Mock<ILogger<AnalyticsController>> _loggerMock;
    private readonly AnalyticsController _controller;

    public AnalyticsControllerTests()
    {
        _cosmosMock = new Mock<IAzureCosmosDbService>();
        _loggerMock = new Mock<ILogger<AnalyticsController>>();
        _controller = new AnalyticsController(_cosmosMock.Object, _loggerMock.Object);

        // Setup TempData
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        _controller.TempData = tempData;

        // Setup a fake user for authorization
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithCorrectStatistics_WhenStudentsExist()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow.AddMonths(-1) },
            new() { Id = "2", FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow.AddMonths(-2) },
            new() { Id = "3", FirstName = "Bob", LastName = "Johnson", Email = "bob@test.com", EnrolmentStatus = "Inactive", CreatedAt = DateTime.UtcNow.AddMonths(-3) },
            new() { Id = "4", FirstName = "Alice", LastName = "Williams", Email = "alice@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow.AddMonths(-4) },
            new() { Id = "5", FirstName = "Charlie", LastName = "Brown", Email = "charlie@test.com", EnrolmentStatus = "Inactive", CreatedAt = DateTime.UtcNow.AddMonths(-5) }
        };

        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 1000)).ReturnsAsync(students);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;

        Assert.Equal(5, _controller.ViewBag.Total);
        Assert.Equal(3, _controller.ViewBag.Active);
        Assert.Equal(2, _controller.ViewBag.Inactive);
        Assert.Equal(60.0, _controller.ViewBag.ActivePercentage);
        Assert.Equal(40.0, _controller.ViewBag.InactivePercentage);

        var recentStudents = _controller.ViewBag.RecentStudents as List<Student>;
        recentStudents.Should().NotBeNull();
        recentStudents.Should().HaveCount(5);
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithZeroStatistics_WhenNoStudentsExist()
    {
        // Arrange
        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 1000)).ReturnsAsync(new List<Student>());

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;

        Assert.Equal(0, _controller.ViewBag.Total);
        Assert.Equal(0, _controller.ViewBag.Active);
        Assert.Equal(0, _controller.ViewBag.Inactive);
        Assert.Equal(0, _controller.ViewBag.ActivePercentage);
        Assert.Equal(0, _controller.ViewBag.InactivePercentage);
    }

    [Fact]
    public async Task Index_ShouldCalculateMonthlyRegistrationsCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var students = new List<Student>();

        // Add 3 students from current month
        for (int i = 0; i < 3; i++)
        {
            students.Add(new Student
            {
                Id = i.ToString(),
                FirstName = $"Test{i}",
                LastName = "User",
                Email = $"test{i}@test.com",
                EnrolmentStatus = "Active",
                CreatedAt = now
            });
        }

        // Add 2 students from last month
        for (int i = 0; i < 2; i++)
        {
            students.Add(new Student
            {
                Id = (i + 10).ToString(),
                FirstName = $"Old{i}",
                LastName = "User",
                Email = $"old{i}@test.com",
                EnrolmentStatus = "Active",
                CreatedAt = now.AddMonths(-1)
            });
        }

        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 1000)).ReturnsAsync(students);

        // Act
        var result = await _controller.Index();

        // Assert
        var counts = _controller.ViewBag.Counts as List<int>;
        counts.Should().NotBeNull();

        // The last element should be current month's count (3)
        counts.Last().Should().Be(3);
        // The second to last should be last month's count (2)
        counts[counts.Count - 2].Should().Be(2);
    }

    [Fact]
    public async Task Index_ShouldAnalyzeEmailDomainsCorrectly()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@gmail.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", FirstName = "Jane", LastName = "Smith", Email = "jane@gmail.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow },
            new() { Id = "3", FirstName = "Bob", LastName = "Johnson", Email = "bob@outlook.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow },
            new() { Id = "4", FirstName = "Alice", LastName = "Williams", Email = "alice@yahoo.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow },
            new() { Id = "5", FirstName = "Charlie", LastName = "Brown", Email = "charlie@gmail.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow }
        };

        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 1000)).ReturnsAsync(students);

        // Act
        var result = await _controller.Index();

        // Assert
        var topDomains = _controller.ViewBag.TopDomains as List<string>;
        var domainCounts = _controller.ViewBag.DomainCounts as List<int>;

        topDomains.Should().NotBeNull();
        domainCounts.Should().NotBeNull();

        // gmail.com should be first with count 3
        topDomains.First().Should().Be("gmail.com");
        domainCounts.First().Should().Be(3);
    }

    [Fact]
    public async Task Index_ShouldHandleExceptionGracefully_WhenCosmosDbFails()
    {
        // Arrange
        var exception = new Exception("Database connection failed");
        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 1000))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;

        // Verify that TempData contains the error message
        Assert.Equal("Failed to load analytics data.", _controller.TempData["Error"]);

        // Verify logger was called with error level
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}