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
using Moq;
using Xunit;

namespace CourseService.Application.Tests;

public class CourseEnrollmentServiceTests
{
    [Theory, AutoMoqData]
    public async Task EnrollStudentAsync_ReturnsTrue_And_CallsAddEnrollment_When_Valid(
        Mock<ICourseRepository> repoMock)
    {
        // Arrange
        var courseId = 1;
        var studentId = 2;
        var instructorUserId = 3;

        var course = new Course
        {
            CourseId = courseId,
            InstructorUserId = instructorUserId
        };

        repoMock.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        repoMock.Setup(r => r.IsStudentEnrolledAsync(courseId, studentId))
            .ReturnsAsync(false);

        repoMock.Setup(r => r.AddEnrollmentAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = new CourseEnrollmentService(repoMock.Object);

        // Act
        var result = await sut.EnrollStudentAsync(courseId, studentId, instructorUserId);

        // Assert
        Assert.True(result);
        repoMock.Verify(r => r.AddEnrollmentAsync(It.Is<CourseEnrollment>(e =>
            e.CourseId == courseId && e.UserId == studentId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task EnrollStudentAsync_ThrowsBadRequest_When_CourseNotFound(
        Mock<ICourseRepository> repoMock)
    {
        // Arrange
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var sut = new CourseEnrollmentService(repoMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => sut.EnrollStudentAsync(10, 20, 30));
        repoMock.Verify(r => r.AddEnrollmentAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task EnrollStudentAsync_ThrowsBadRequest_When_NotCourseOwner(
        Mock<ICourseRepository> repoMock)
    {
        // Arrange
        var course = new Course { CourseId = 5, InstructorUserId = 99 };
        repoMock.Setup(r => r.GetByIdAsync(course.CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        var sut = new CourseEnrollmentService(repoMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => sut.EnrollStudentAsync(course.CourseId, 2, instructorUserId: 1));
        repoMock.Verify(r => r.AddEnrollmentAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task EnrollStudentAsync_When_AlreadyEnrolled_AddsEnrollment_BehaviorObserved(
        Mock<ICourseRepository> repoMock)
    {
        // Note: current implementation creates a BadRequestException instance without throwing it when already enrolled.
        // This test documents the observed behavior: AddEnrollmentAsync is still called.
        var courseId = 11;
        var studentId = 22;
        var instructorUserId = 33;

        var course = new Course { CourseId = courseId, InstructorUserId = instructorUserId };
        repoMock.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);

        repoMock.Setup(r => r.IsStudentEnrolledAsync(courseId, studentId))
            .ReturnsAsync(true);

        repoMock.Setup(r => r.AddEnrollmentAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var sut = new CourseEnrollmentService(repoMock.Object);

        var result = await sut.EnrollStudentAsync(courseId, studentId, instructorUserId);

        Assert.True(result);
        repoMock.Verify(r => r.AddEnrollmentAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetEnrolledCoursesByStudentAsync_Returns_MappedCourseDtos(
        Mock<ICourseRepository> repoMock)
    {
        // Arrange
        var studentId = 44;
        var courses = new List<Course>
        {
            new Course { CourseId = 1, CourseName = "C1", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(1) },
            new Course { CourseId = 2, CourseName = "C2", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(2) }
        };

        repoMock.Setup(r => r.GetEnrolledCoursesByStudentIdAsync(studentId))
            .ReturnsAsync(courses);

        var sut = new CourseEnrollmentService(repoMock.Object);

        // Act
        var result = await sut.GetEnrolledCoursesByStudentAsync(studentId);

        // Assert
        Assert.Equal(courses.Count, result.Count);
        Assert.Equal(courses[0].CourseName, result[0].CourseName);
        Assert.Equal(courses[1].CourseName, result[1].CourseName);
    }
}