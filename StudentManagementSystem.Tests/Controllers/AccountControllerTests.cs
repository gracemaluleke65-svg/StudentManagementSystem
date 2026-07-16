using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StudentManagementSystem.Controllers;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.Security.Claims;
using Xunit;

namespace StudentManagementSystem.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IAzureCosmosDbService> _cosmosMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<AccountController>> _loggerMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _cosmosMock = new Mock<IAzureCosmosDbService>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AccountController>>();
        _controller = new AccountController(_cosmosMock.Object, _configMock.Object, _loggerMock.Object);

        // Setup TempData
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        _controller.TempData = tempData;
    }

    [Fact]
    public void Login_ShouldReturnViewWithClientIds()
    {
        // Arrange
        _configMock.Setup(c => c["Authentication:Google:ClientId"]).Returns("google-id");
        _configMock.Setup(c => c["Authentication:GitHub:ClientId"]).Returns("github-id");

        // Act
        var result = _controller.Login();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        Assert.Equal("google-id", _controller.ViewBag.GoogleClientId);
        Assert.Equal("github-id", _controller.ViewBag.GitHubClientId);
    }

    [Fact]
    public void ExternalLogin_WithEmptyProvider_ShouldRedirectToLoginWithError()
    {
        // Act
        var result = _controller.ExternalLogin("");

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Login");
        _controller.TempData["Error"].Should().NotBeNull();
    }

    [Fact]
    public async Task ExternalLoginCallback_WithRemoteError_ShouldRedirectToLoginWithError()
    {
        // Act
        var result = await _controller.ExternalLoginCallback(remoteError: "User denied");

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Login");
        _controller.TempData["Error"].Should().Be("Authentication failed: User denied");
    }
}
