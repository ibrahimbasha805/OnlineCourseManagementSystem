using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Abstractions;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Common;
using UserService.WebApi.Controllers;
using Xunit;

namespace UserService.WebApi.Tests;

public class EnrollmentsControllerTests
{
    [Theory, AutoMoqData]
    public async Task Enroll_ReturnsOk_AndForwardsAuthorizationHeader(
        [Frozen] Mock<IEnrollService> enrollServiceMock,
        EnrollCourseDto dto,
        CourseDto course,
        EnrollmentsController controller)
    {
        // Arrange
        var forwarded = "Bearer sometoken";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Add("Authorization", forwarded);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        enrollServiceMock
            .Setup(s => s.EnrollStudentAsync(dto, forwarded))
            .ReturnsAsync(course);

        // Act
        var result = await controller.Enroll(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeSameAs(course);

        enrollServiceMock.Verify(s => s.EnrollStudentAsync(dto, forwarded), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Enroll_PropagatesException_FromEnrollService(
        [Frozen] Mock<IEnrollService> enrollServiceMock,
        EnrollCourseDto dto,
        EnrollmentsController controller)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var ex = new CourseServiceClientException("fail", 500, "{ \"error\": \"x\" }");
        enrollServiceMock
            .Setup(s => s.EnrollStudentAsync(dto, (string?)null))
            .ThrowsAsync(ex);

        // Act / Assert
        await Assert.ThrowsAsync<CourseServiceClientException>(() => controller.Enroll(dto));

        enrollServiceMock.Verify(s => s.EnrollStudentAsync(dto, (string?)null), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Enroll_ForwardsNullAuthorization_WhenHeaderMissing(
        [Frozen] Mock<IEnrollService> enrollServiceMock,
        EnrollCourseDto dto,
        CourseDto course,
        EnrollmentsController controller)
    {
        // Arrange - no Authorization header set
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        enrollServiceMock
            .Setup(s => s.EnrollStudentAsync(dto, (string?)null))
            .ReturnsAsync(course);

        // Act
        var result = await controller.Enroll(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeSameAs(course);

        enrollServiceMock.Verify(s => s.EnrollStudentAsync(dto, (string?)null), Times.Once);
    }
}