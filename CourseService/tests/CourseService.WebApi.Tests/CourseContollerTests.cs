using System.Security.Claims;
using AutoFixture.Xunit2;
using CourseService.Application.Abstractions;
using CourseService.Application.DTOs;
using CourseService.Common;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CourseService.WebApi.Tests;

public class CoursesControllerTests
{
    [Theory, AutoMoqData]
    public async Task CreateCourse_Returns_Created_When_Valid(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock,
        CreateCourseDto request)
    {
        // Arrange
        var newCourseId = 123;
        createValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // valid

        courseServiceMock
            .Setup(s => s.CreateCourseAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCourseId);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var result = await sut.CreateCourse(request, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CourseService.WebApi.Controllers.CoursesController.GetCourseById), created.ActionName);
        Assert.True(created.RouteValues!.ContainsKey("id"));
        Assert.Equal(newCourseId, created.RouteValues["id"]);
    }

    [Theory, AutoMoqData]
    public async Task CreateCourse_Returns_ValidationProblem_When_Invalid(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock,
        CreateCourseDto request)
    {
        // Arrange
        var failures = new[] { new ValidationFailure("Name", "Required") };
        createValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var result = await sut.CreateCourse(request, CancellationToken.None);

        // Assert
        var obj = Assert.IsType<ObjectResult>(result);
        Assert.True(((Microsoft.AspNetCore.Mvc.ValidationProblemDetails)((Microsoft.AspNetCore.Mvc.ObjectResult)result).Value).Errors.Count() == 1);
        
    }

    [Theory, AutoMoqData]
    public async Task GetCourseById_Returns_NotFound_When_Null(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock)
    {
        // Arrange
        int id = 10;
        courseServiceMock
            .Setup(s => s.GetCourseByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseDto?)null);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var result = await sut.GetCourseById(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Theory, AutoMoqData]
    public async Task GetCourseById_Returns_Ok_When_Found(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock,
        CourseDto expected,
        int courseId)
    {
        // Arrange        
        courseServiceMock
            .Setup(s => s.GetCourseByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var result = await sut.GetCourseById(courseId, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Theory, AutoMoqData]
    public async Task UpdateCourse_Returns_NoContent_When_Updated(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock,
        UpdateCourseDto request)
    {
        // Arrange
        int id = 7;
        updateValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // valid

        courseServiceMock
            .Setup(s => s.UpdateCourseAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // must set User with NameIdentifier claim to avoid NullReference when converting
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }))
            }
        };

        // Act
        var result = await sut.UpdateCourse(id, request, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Theory, AutoMoqData]
    public async Task UpdateCourse_Returns_NotFound_When_NotUpdated(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> create_validatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock,
        UpdateCourseDto request)
    {
        // Arrange
        int id = 99;
        updateValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // valid

        courseServiceMock
            .Setup(s => s.UpdateCourseAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            create_validatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") }))
            }
        };

        // Act
        var result = await sut.UpdateCourse(id, request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Theory, AutoMoqData]
    public async Task UpdateCourse_Returns_ValidationProblem_When_Invalid(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock,
        UpdateCourseDto request)
    {
        // Arrange
        var failures = new[] { new ValidationFailure("Title", "Required") };
        updateValidatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var result = await sut.UpdateCourse(1, request, CancellationToken.None);

        // Assert
        var obj = Assert.IsType<ObjectResult>(result);
        Assert.True(((Microsoft.AspNetCore.Mvc.ValidationProblemDetails)((Microsoft.AspNetCore.Mvc.ObjectResult)result).Value).Errors.Count() == 1);
    }

    [Theory, AutoMoqData]
    public async Task DeleteCourse_Returns_NoContent_When_Deleted(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock)
    {
        // Arrange
        int id = 5;
        courseServiceMock
            .Setup(s => s.DeleteCourseAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var result = await sut.DeleteCourse(id, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Theory, AutoMoqData]
    public async Task DeleteCourse_Returns_NotFound_When_NotDeleted(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock)
    {
        // Arrange
        int id = 1234;
        courseServiceMock
            .Setup(s => s.DeleteCourseAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var result = await sut.DeleteCourse(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Theory, AutoMoqData]
    public async Task Search_Returns_Ok_With_PagedResult(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock,
        List<SearchCourseDto> searchCourseDtos)
    {
        // Arrange
        var paged = new PagedResult<SearchCourseDto>
        {
            Items = searchCourseDtos,
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        courseServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(paged);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var actionResult = await sut.Search(DateTime.Now.AddDays(-10), DateTime.Now,"basha" ,1, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(paged, ok.Value);
    }

    [Theory, AutoMoqData]
    public async Task GetAllCourses_Returns_Ok_With_List(
        Mock<ILogger<CourseService.WebApi.Controllers.CoursesController>> loggerMock,
        Mock<IValidator<CreateCourseDto>> createValidatorMock,
        Mock<IValidator<UpdateCourseDto>> updateValidatorMock,
        Mock<ICourseService> courseServiceMock,
        List<AllCoursesDto> list)
    {
        // Arrange       

        courseServiceMock
            .Setup(s => s.GetAllCourseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var sut = new CourseService.WebApi.Controllers.CoursesController(
            loggerMock.Object,
            createValidatorMock.Object,
            updateValidatorMock.Object,
            courseServiceMock.Object);

        // Act
        var result = await sut.GetAllCourses(CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, ok.Value);
    }
}