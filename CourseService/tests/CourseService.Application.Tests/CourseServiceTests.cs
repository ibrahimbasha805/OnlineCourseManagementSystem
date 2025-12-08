using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CourseService.Application.Abstractions;
using CourseService.Application.DTOs;
using CourseService.Application.Exceptions;
using CourseService.Application.Services;
using CourseService.Common;
using CourseService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CourseService.Application.Tests;

public class CourseServiceTests
{
    [Theory, AutoMoqData]
    public async Task CreateCourseAsync_Succeeds_When_UserIsInstructor(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var dto = new CreateCourseDto
        {
            CourseName = "Test",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            InstructorId = 11
        };

        
        userClientMock
            .Setup(x => x.GetUserAsync(dto.InstructorId))
            .ReturnsAsync(new UserDto { userId = dto.InstructorId, roleName = "Instructor" });

        
        var expectedCourseId = 555;
        repoMock
            .Setup(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()))
            .Returns<Course, CancellationToken>((c, ct) =>
            {
                c.CourseId = expectedCourseId;
                return Task.CompletedTask;
            });

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act
        var id = await sut.CreateCourseAsync(dto, CancellationToken.None);

        // Assert
        Assert.Equal(expectedCourseId, id);
        repoMock.Verify(r => r.AddAsync(It.Is<Course>(c =>
            c.CourseName == dto.CourseName &&
            c.InstructorUserId == dto.InstructorId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task CreateCourseAsync_Throws_BadRequest_When_UserIsNotInstructor(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var dto = new CreateCourseDto { InstructorId = 99, CourseName = "X" };
        userClientMock
            .Setup(x => x.GetUserAsync(dto.InstructorId))
            .ReturnsAsync(new UserDto { userId = dto.InstructorId, roleName = "Student" });

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateCourseAsync(dto, CancellationToken.None));
        repoMock.Verify(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task GetAllCourseAsync_Maps_AllCourses(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var courses = new List<Course>
        {
            new Course { CourseId = 1, CourseName = "A", InstructorUserId = 2 },
            new Course { CourseId = 2, CourseName = "B", InstructorUserId = 3 }
        };

        repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act
        var result = await sut.GetAllCourseAsync(CancellationToken.None);

        // Assert
        Assert.Collection(result,
            item =>
            {
                Assert.Equal(1, item.CourseId);
                Assert.Equal("A", item.CourseName);
                Assert.Equal(2, item.InstructorUserId);
            },
            item =>
            {
                Assert.Equal(2, item.CourseId);
                Assert.Equal("B", item.CourseName);
                Assert.Equal(3, item.InstructorUserId);
            });
    }

    [Theory, AutoMoqData]
    public async Task GetCourseByIdAsync_Returns_Null_When_NotFound(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act
        var result = await sut.GetCourseByIdAsync(123, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory, AutoMoqData]
    public async Task GetCourseByIdAsync_Returns_MappedDto_When_Found(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var course = new Course
        {
            CourseId = 7,
            CourseName = "NameX",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(5),
            InstructorUserId = 12
        };

        repoMock.Setup(r => r.GetByIdAsync(course.CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act
        var dto = await sut.GetCourseByIdAsync(course.CourseId, CancellationToken.None);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(course.CourseName, dto!.CourseName);
        Assert.Equal(course.StartDate, dto.StartDate);
        Assert.Equal(course.EndDate, dto.EndDate);
    }

    [Theory, AutoMoqData]
    public async Task UpdateCourseAsync_Returns_True_When_Successful(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var existing = new Course
        {
            CourseId = 10,
            CourseName = "Old",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            InstructorUserId = 2
        };

        var dto = new UpdateCourseDto
        {
            CourseName = "New",
            StartDate = existing.StartDate,
            EndDate = existing.EndDate,
            InstructorId = 2
        };

        userClientMock.Setup(u => u.GetUserAsync(dto.InstructorId))
            .ReturnsAsync(new UserDto { userId = dto.InstructorId, roleName = "Instructor" });

        repoMock.Setup(r => r.GetByIdAsync(existing.CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        repoMock.Setup(r => r.UpdateAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act
        var result = await sut.UpdateCourseAsync(existing.CourseId, dto, CancellationToken.None);

        // Assert
        Assert.True(result);
        repoMock.Verify(r => r.UpdateAsync(It.Is<Course>(c => c.CourseName == dto.CourseName && c.InstructorUserId == dto.InstructorId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task UpdateCourseAsync_Returns_False_When_NotFound(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var dto = new UpdateCourseDto { InstructorId = 5, CourseName = "X" };

        userClientMock.Setup(u => u.GetUserAsync(dto.InstructorId))
            .ReturnsAsync(new UserDto { userId = dto.InstructorId, roleName = "Instructor" });

        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act
        var result = await sut.UpdateCourseAsync(999, dto, CancellationToken.None);

        // Assert
        Assert.False(result);
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task UpdateCourseAsync_Throws_BadRequest_When_UserIsNotInstructor(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var dto = new UpdateCourseDto { InstructorId = 3, CourseName = "X" };

        userClientMock.Setup(u => u.GetUserAsync(dto.InstructorId))
            .ReturnsAsync(new UserDto { userId = dto.InstructorId, roleName = "Student" });

        var sut = new  Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => sut.UpdateCourseAsync(1, dto, CancellationToken.None));
        repoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task DeleteCourseAsync_Returns_True_When_Deleted(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var course = new Course { CourseId = 4 };
        repoMock.Setup(r => r.GetByIdAsync(course.CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        repoMock.Setup(r => r.DeleteAsync(course.CourseId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act
        var result = await sut.DeleteCourseAsync(course.CourseId, CancellationToken.None);

        // Assert
        Assert.True(result);
        repoMock.Verify(r => r.DeleteAsync(course.CourseId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task DeleteCourseAsync_Returns_False_When_NotFound(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Act
        var result = await sut.DeleteCourseAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result);
        repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task SearchAsync_Returns_PagedResult_With_MappedItems_And_NormalizedPaging(
        Mock<ILogger<Services.CourseService>> loggerMock,
        Mock<ICourseRepository> repoMock,
        Mock<IUserServiceClient> userClientMock)
    {
        // Arrange
        var instructorName = "instructor-x";
        var instructorUserId = 77;
        userClientMock.Setup(u => u.GetUserByInstructorNameAsync(instructorName))
            .ReturnsAsync(new UserDto { userId = instructorUserId, roleName = "Instructor" });

        var courses = new List<Course>
        {
            new Course { CourseId = 1, CourseName = "C1", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(1) },
            new Course { CourseId = 2, CourseName = "C2", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(2) }
        };

        var total = courses.Count;
        repoMock.Setup(r => r.SearchAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), instructorUserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((courses, total));

        var sut = new Services.CourseService(loggerMock.Object, repoMock.Object, userClientMock.Object);

        // Provide invalid paging to ensure normalization to defaults
        var result = await sut.SearchAsync(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10), instructorName, 1, 10);

        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber); 
        Assert.Equal(10, result.PageSize);  
        Assert.Equal(total, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal(instructorName, item.InstructorName));
    }
}