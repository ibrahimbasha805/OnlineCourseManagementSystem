using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Abstractions;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Enum;
using Xunit;

namespace UserService.Application.Tests;

public class UserServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IUserRepository> _repoMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<Services.UserService>> _loggerMock;
    private readonly Services.UserService _sut;

    public UserServiceTests()
    {
        _fixture = new Fixture();
        _repoMock = new Mock<IUserRepository>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<Services.UserService>>();

        _sut = new Services.UserService(_loggerMock.Object, _configMock.Object, _repoMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenUserDoesNotExist_ReturnsUserDto()
    {
        // Arrange
        var dto = _fixture.Build<RegisterUserDto>()
            .With(x => x.UserName, "alice")
            .With(x => x.Password, "P@ssw0rd")
            .Create();

        _repoMock.Setup(r => r.GetByUserNameAsync(dto.UserName)).ReturnsAsync((User?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(42);

        // Act
        var result = await _sut.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(42);
        result.Name.Should().Be(dto.Name);
        result.RoleName.Should().Be(dto.Role.ToString());
    }

    [Fact]
    public async Task RegisterAsync_WhenUserAlreadyExists_ThrowsBadRequestException()
    {
        // Arrange
        var dto = _fixture.Build<RegisterUserDto>()
            .With(x => x.UserName, "bob")
            .Create();

        var existing = new User("Bob", dto.UserName, "hashed", UserRole.Instructor);
        _repoMock.Setup(r => r.GetByUserNameAsync(dto.UserName)).ReturnsAsync(existing);

        // Act / Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RegisterAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        var dto = _fixture.Build<LoginRequestDto>()
            .With(x => x.UserName, "unknown")
            .Create();

        _repoMock.Setup(r => r.GetByUserNameAsync(dto.UserName)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordDoesNotMatch_ReturnsNull()
    {
        // Arrange
        var dto = _fixture.Build<LoginRequestDto>()
            .With(x => x.UserName, "charlie")
            .With(x => x.Password, "right-password")
            .Create();

        var user = new User("Charlie", dto.UserName, BCrypt.Net.BCrypt.HashPassword("other-password"), UserRole.Student);
        _repoMock.Setup(r => r.GetByUserNameAsync(dto.UserName)).ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsLoginResponse()
    {
        // Arrange
        var dto = _fixture.Build<LoginRequestDto>()
            .With(x => x.UserName, "dave")
            .With(x => x.Password, "correct-password")
            .Create();

        // configure JWT settings required for token generation
        _configMock.Setup(c => c["Jwt:Key"]).Returns("very-long-test-key-which-is-secret");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("test-audience");

        var hashed = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User("Dave", dto.UserName, hashed, UserRole.Instructor);
        _repoMock.Setup(r => r.GetByUserNameAsync(dto.UserName)).ReturnsAsync(user);

        // need a sut that uses configured IConfiguration mock
        var sutWithConfig = new Services.UserService(_loggerMock.Object, _configMock.Object, _repoMock.Object);

        // Act
        var result = await sutWithConfig.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        result.UserName.Should().Be(user.UserName);
        result.Role.Should().Be(user.Role.ToString());
        result.UserId.Should().Be(user.UserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(123)]
    public async Task GetByIdAsync_VariousIds_ReturnsExpectedResult(int id)
    {
        // Arrange
        if (id == 0)
        {
            _repoMock.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync((User?)null);
        }
        else
        {
            var user = new User("Eve", "eve", BCrypt.Net.BCrypt.HashPassword("pw"), UserRole.Student);
            _repoMock.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync(user);
        }

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        if (id == 0)
            result.Should().BeNull();
        else
            result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetByNameData))]
    public async Task GetByNameAsync_MemberData_ReturnsExpected(string inputName, User? repoReturn)
    {
        // Arrange
        _repoMock.Setup(r => r.GetByNameAsync(inputName)).ReturnsAsync(repoReturn);

        // Act
        var result = await _sut.GetByNameAsync(inputName);

        // Assert
        if (repoReturn == null)
            result.Should().BeNull();
        else
        {
            result.Should().NotBeNull();
            result!.Name.Should().Be(repoReturn.Name);
            result.RoleName.Should().Be(repoReturn.Role.ToString());
        }
    }

    public static IEnumerable<object[]> GetByNameData()
    {
        yield return new object[] { "non-existent", null };
        yield return new object[] { "Frank", new User("Frank", "frank", BCrypt.Net.BCrypt.HashPassword("pw"), UserRole.Instructor) };
    }

    [Theory]
    [MemberData(nameof(GetAllData))]
    public async Task GetAllAsync_MemberData_ReturnsMappedDtos(List<User> repoUsers)
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllUsers()).ReturnsAsync(repoUsers);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(repoUsers.Count);
        for (var i = 0; i < repoUsers.Count; i++)
        {
            result[i].Name.Should().Be(repoUsers[i].Name);
            result[i].RoleName.Should().Be(repoUsers[i].Role.ToString());
        }
    }

    public static IEnumerable<object[]> GetAllData()
    {
        yield return new object[] { new List<User>() };
        yield return new object[] {
            new List<User>
            {
                new User("A", "a", BCrypt.Net.BCrypt.HashPassword("1"), UserRole.Student),
                new User("B", "b", BCrypt.Net.BCrypt.HashPassword("2"), UserRole.Instructor)
            }
        };
    }
}