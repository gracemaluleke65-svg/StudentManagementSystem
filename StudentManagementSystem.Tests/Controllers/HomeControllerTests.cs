using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using StudentManagementSystem.Controllers;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using Xunit;

namespace StudentManagementSystem.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<ILogger<HomeController>> _loggerMock;
    private readonly Mock<IAzureCosmosDbService> _cosmosMock;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _loggerMock = new Mock<ILogger<HomeController>>();
        _cosmosMock = new Mock<IAzureCosmosDbService>();
        _controller = new HomeController(_loggerMock.Object, _cosmosMock.Object);

        // Setup TempData
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        _controller.TempData = tempData;
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithStudentStatistics_WhenStudentsExist()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow },
            new() { Id = "3", FirstName = "Bob", LastName = "Johnson", Email = "bob@test.com", EnrolmentStatus = "Inactive", CreatedAt = DateTime.UtcNow },
            new() { Id = "4", FirstName = "Alice", LastName = "Williams", Email = "alice@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow }
        };

        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 1000)).ReturnsAsync(students);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;

        Assert.Equal(4, _controller.ViewBag.StudentCount);
        Assert.Equal(3, _controller.ViewBag.ActiveCount);
        Assert.Equal(1, _controller.ViewBag.AdminCount);
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

        Assert.Equal(0, _controller.ViewBag.StudentCount);
        Assert.Equal(0, _controller.ViewBag.ActiveCount);
        Assert.Equal(1, _controller.ViewBag.AdminCount); // AdminCount is always 1 in success path
    }

    [Fact]
    public async Task Index_ShouldHandleException_WhenCosmosDbFails()
    {
        // Arrange
        var exception = new Exception("Database error");
        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 1000))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;

        // When exception occurs, all counts are set to 0
        Assert.Equal(0, _controller.ViewBag.StudentCount);
        Assert.Equal(0, _controller.ViewBag.ActiveCount);
        Assert.Equal(0, _controller.ViewBag.AdminCount);

        // Note: The controller's catch block does NOT log the exception
        // So we don't verify logger was called
        // If you want logging, you would need to add it to the controller
    }

    [Fact]
    public async Task Index_ShouldCalculateActiveCountCorrectly_WhenMixedStatuses()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow },
            new() { Id = "2", FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow },
            new() { Id = "3", FirstName = "Bob", LastName = "Johnson", Email = "bob@test.com", EnrolmentStatus = "Inactive", CreatedAt = DateTime.UtcNow },
            new() { Id = "4", FirstName = "Alice", LastName = "Williams", Email = "alice@test.com", EnrolmentStatus = "Inactive", CreatedAt = DateTime.UtcNow },
            new() { Id = "5", FirstName = "Charlie", LastName = "Brown", Email = "charlie@test.com", EnrolmentStatus = "Active", CreatedAt = DateTime.UtcNow }
        };

        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 1000)).ReturnsAsync(students);

        // Act
        var result = await _controller.Index();

        // Assert
        Assert.Equal(5, _controller.ViewBag.StudentCount);
        Assert.Equal(3, _controller.ViewBag.ActiveCount);
    }

    [Fact]
    public void Privacy_ShouldReturnView()
    {
        // Act
        var result = _controller.Privacy();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ShouldReturnViewWithErrorViewModel()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-123";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = _controller.Error();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ErrorViewModel>().Subject;
        model.RequestId.Should().Be("trace-123");
        model.ShowRequestId.Should().BeTrue();
    }

    [Fact]
    public void Error_ShouldShowRequestId_WhenTraceIdentifierIsProvided()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "custom-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = _controller.Error();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ErrorViewModel>().Subject;
        model.RequestId.Should().Be("custom-trace-id");
        model.ShowRequestId.Should().BeTrue();
    }
}