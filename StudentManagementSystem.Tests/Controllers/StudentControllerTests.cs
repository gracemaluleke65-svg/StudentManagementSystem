using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using StudentManagementSystem.Controllers;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;
using System.Security.Claims;
using Xunit;

namespace StudentManagementSystem.Tests.Controllers;

public class StudentControllerTests
{
    private readonly Mock<IAzureCosmosDbService> _cosmosMock;
    private readonly Mock<IAzureBlobStorageService> _blobMock;
    private readonly Mock<ILogger<StudentController>> _loggerMock;
    private readonly StudentController _controller;

    public StudentControllerTests()
    {
        _cosmosMock = new Mock<IAzureCosmosDbService>();
        _blobMock = new Mock<IAzureBlobStorageService>();
        _loggerMock = new Mock<ILogger<StudentController>>();
        _controller = new StudentController(_cosmosMock.Object, _blobMock.Object, _loggerMock.Object);

        // Setup a fake user for authorization
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));
        
        // Setup TempData
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        _controller.TempData = tempData;
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithStudents_WhenCalled()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { Id = "1", FirstName = "John", LastName = "Doe" },
            new() { Id = "2", FirstName = "Jane", LastName = "Smith" }
        };
        _cosmosMock.Setup(s => s.GetTotalStudentCountAsync()).ReturnsAsync(2);
        _cosmosMock.Setup(s => s.GetStudentsAsync(1, 10)).ReturnsAsync(students);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Student>>().Subject;
        model.Should().HaveCount(2);
        Assert.Equal(2, _controller.ViewBag.TotalCount);
    }

    [Fact]
    public void Create_Get_ShouldReturnView()
    {
        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_ExistingEmail_ShouldReturnViewWithError()
    {
        // Arrange
        var model = new StudentViewModel
        {
            FirstName = "Test",
            LastName = "User",
            Email = "existing@example.com",
            MobileNumber = "1234567890",
            EnrolmentStatus = "Active"
        };
        _cosmosMock.Setup(s => s.StudentExistsAsync(model.Email)).ReturnsAsync(true);

        // Act
        var result = await _controller.Create(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(model);
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState.ContainsKey("Email").Should().BeTrue();
    }

    [Fact]
    public async Task Edit_Get_ValidId_ShouldReturnViewWithModel()
    {
        // Arrange
        var student = new Student { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        _cosmosMock.Setup(s => s.GetStudentAsync("1")).ReturnsAsync(student);

        // Act
        var result = await _controller.Edit("1");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<StudentViewModel>().Subject;
        model.Id.Should().Be("1");
        model.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task Edit_Get_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _cosmosMock.Setup(s => s.GetStudentAsync("99")).ReturnsAsync((Student?)null);

        // Act
        var result = await _controller.Edit("99");

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Search_ShouldReturnPartialViewWithFilteredStudents()
    {
        // Arrange
        var searchTerm = "john";
        var students = new List<Student>
        {
            new() { Id = "1", FirstName = "John", LastName = "Doe" }
        };
        _cosmosMock.Setup(s => s.SearchStudentsAsync(searchTerm, 1, 10)).ReturnsAsync(students);

        // Act
        var result = await _controller.Search(searchTerm);

        // Assert
        var partialView = result.Should().BeOfType<PartialViewResult>().Subject;
        partialView.ViewName.Should().Be("_StudentList");
        var model = partialView.Model.Should().BeAssignableTo<IEnumerable<Student>>().Subject;
        model.Should().HaveCount(1);
    }
}
