using System.Security.Claims;
using AutoFixture.Xunit2;
using CourseService.Application.Abstractions;
using CourseService.Application.DTOs;
using CourseService.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseService.WebApi.Tests;

public class CourseEnrollmentControllerTests
{
    [Theory, AutoMoqData]
    public async Task Enroll_Returns_Ok_When_InstructorIdClaimValid(
        Mock<ICourseEnrollmentService> enrollServiceMock,
        EnrollRequestDto request)
    {
        // Arrange
        var courseId = 42;
        var instructorId = 7;

        enrollServiceMock
            .Setup(s => s.EnrollStudentAsync(courseId, request.StudentId, instructorId))
            .ReturnsAsync(true);

        var sut = new CourseService.WebApi.Controllers.CourseEnrollmentController(enrollServiceMock.Object);

        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, instructorId.ToString()),
                    new Claim(ClaimTypes.Role, "Instructor")
                }))
            }
        };

        // Act
        var result = await sut.Enroll(courseId, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        // anonymous object contains property "success" == true
        var prop = ok.Value!.GetType().GetProperty("success");
        Assert.NotNull(prop);
        Assert.True((bool)prop.GetValue(ok.Value)!);

        enrollServiceMock.Verify(s => s.EnrollStudentAsync(courseId, request.StudentId, instructorId), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Enroll_Returns_Unauthorized_When_InstructorIdClaimInvalid(
        Mock<ICourseEnrollmentService> enrollServiceMock,
        EnrollRequestDto request)
    {
        // Arrange
        var courseId = 42;

        var sut = new CourseService.WebApi.Controllers.CourseEnrollmentController(enrollServiceMock.Object);

        // Provide an invalid NameIdentifier (non-integer)
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "not-an-int"),
                    new Claim(ClaimTypes.Role, "Instructor")
                }))
            }
        };

        // Act
        var result = await sut.Enroll(courseId, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);

        // Ensure service was not called
        enrollServiceMock.Verify(s => s.EnrollStudentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task GetEnrolledCoursesForStudent_Returns_Ok_With_Courses_When_Valid(
        Mock<ICourseEnrollmentService> enrollServiceMock,
        List<CourseDto> expectedCourses)
    {
        // Arrange
        var studentId = 99;

        enrollServiceMock
            .Setup(s => s.GetEnrolledCoursesByStudentAsync(studentId))
            .ReturnsAsync(expectedCourses);

        var sut = new CourseService.WebApi.Controllers.CourseEnrollmentController(enrollServiceMock.Object);

        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, studentId.ToString()),
                    new Claim(ClaimTypes.Role, "Student")
                }))
            }
        };

        // Act
        var result = await sut.GetEnrolledCoursesForStudent();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedCourses, ok.Value);
        enrollServiceMock.Verify(s => s.GetEnrolledCoursesByStudentAsync(studentId), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetEnrolledCoursesForStudent_Returns_Unauthorized_When_StudentIdClaimInvalid(
        Mock<ICourseEnrollmentService> enrollServiceMock)
    {
        // Arrange
        var sut = new CourseService.WebApi.Controllers.CourseEnrollmentController(enrollServiceMock.Object);

        // Missing or invalid NameIdentifier
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "abc"), // non-integer
                    new Claim(ClaimTypes.Role, "Student")
                }))
            }
        };

        // Act
        var result = await sut.GetEnrolledCoursesForStudent();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        enrollServiceMock.Verify(s => s.GetEnrolledCoursesByStudentAsync(It.IsAny<int>()), Times.Never);
    }
}