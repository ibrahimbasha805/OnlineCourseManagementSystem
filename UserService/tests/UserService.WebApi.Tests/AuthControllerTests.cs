using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Abstractions;
using UserService.Application.DTOs;
using UserService.Common;
using UserService.WebApi.Controllers;
using Xunit;

namespace UserService.WebApi.Tests;

public class AuthControllerTests
{
    [Theory, AutoMoqData]
    public async Task Register_ReturnsOk_WithUserDto(
        [Frozen] Mock<IUserService> userServiceMock,
        RegisterUserDto registerDto,
        UserDto expectedUser,
        AuthController controller)
    {
        // Arrange
        userServiceMock
            .Setup(s => s.RegisterAsync(registerDto))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await controller.Register(registerDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeSameAs(expectedUser);
    }

    [Theory, AutoMoqData]
    public async Task Login_ReturnsOk_WhenCredentialsValid(
        [Frozen] Mock<IUserService> userServiceMock,
        LoginRequestDto loginDto,
        LoginResponseDto loginResponse,
        AuthController controller)
    {
        // Arrange
        userServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await controller.Login(loginDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeSameAs(loginResponse);
    }

    [Theory, AutoMoqData]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid(
        [Frozen] Mock<IUserService> userServiceMock,
        LoginRequestDto loginDto,
        AuthController controller)
    {
        // Arrange
        userServiceMock
            .Setup(s => s.LoginAsync(loginDto))
            .ReturnsAsync((LoginResponseDto?)null);

        // Act
        var result = await controller.Login(loginDto);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorized = (UnauthorizedObjectResult)result;
        unauthorized.Value.Should().Be("Invalid username or password.");
    }
}