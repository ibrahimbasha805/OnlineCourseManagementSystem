using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CourseService.Domain.Entities;
using CourseService.Infrastructure.Persistence;
using CourseService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CourseService.Infrastructure.Tests;

public class CourseRepositoryTests
{
    private static CourseDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CourseDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new CourseDbContext(options);
    }

    [Fact]
    public async Task AddAsync_AddsCourseToDatabase()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);
        var repo = new CourseRepository(ctx);

        var course = new Course
        {
            CourseName = "Test Course",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            InstructorUserId = 1
        };

        await repo.AddAsync(course, CancellationToken.None);

        var saved = await ctx.Courses.FirstOrDefaultAsync(c => c.CourseName == "Test Course");
        Assert.NotNull(saved);
        Assert.Equal(course.CourseName, saved!.CourseName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCourse_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        var course = new Course
        {
            CourseName = "ById",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(5),
            InstructorUserId = 2
        };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var repo = new CourseRepository(ctx);
        var found = await repo.GetByIdAsync(course.CourseId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(course.CourseId, found!.CourseId);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCourseInDatabase()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        var course = new Course
        {
            CourseName = "Before",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(3),
            InstructorUserId = 3
        };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var repo = new CourseRepository(ctx);

        course.CourseName = "After";
        await repo.UpdateAsync(course, CancellationToken.None);

        var updated = await ctx.Courses.FindAsync(new object[] { course.CourseId }, CancellationToken.None);
        Assert.Equal("After", updated!.CourseName);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCourse_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        var course = new Course
        {
            CourseName = "ToDelete",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            InstructorUserId = 4
        };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var repo = new CourseRepository(ctx);
        await repo.DeleteAsync(course.CourseId, CancellationToken.None);

        var found = await ctx.Courses.FindAsync(new object[] { course.CourseId }, CancellationToken.None);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        ctx.Courses.Add(new Course
        {
            CourseName = "Keep",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            InstructorUserId = 5
        });
        await ctx.SaveChangesAsync();

        var initialCount = await ctx.Courses.CountAsync();
        var repo = new CourseRepository(ctx);

        await repo.DeleteAsync(9999, CancellationToken.None); // non-existent id

        var finalCount = await ctx.Courses.CountAsync();
        Assert.Equal(initialCount, finalCount);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCoursesOrderedByStartDate()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        var now = DateTime.UtcNow;
        var courses = new[]
        {
            new Course { CourseName = "C1", StartDate = now.AddDays(3), EndDate = now.AddDays(4), InstructorUserId = 1 },
            new Course { CourseName = "C2", StartDate = now.AddDays(1), EndDate = now.AddDays(2), InstructorUserId = 1 },
            new Course { CourseName = "C3", StartDate = now.AddDays(2), EndDate = now.AddDays(3), InstructorUserId = 1 }
        };

        ctx.Courses.AddRange(courses);
        await ctx.SaveChangesAsync();

        var repo = new CourseRepository(ctx);
        var all = await repo.GetAllAsync(CancellationToken.None);

        Assert.Equal(3, all.Count);
        Assert.Equal("C2", all[0].CourseName);
        Assert.Equal("C3", all[1].CourseName);
        Assert.Equal("C1", all[2].CourseName);
    }

    [Fact]
    public async Task SearchAsync_ReturnsFilteredAndPagedResults()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        var baseDate = new DateTime(2025, 1, 1);
        // instructor 10 has 3 courses, only 2 within date window
        var courses = new List<Course>
        {
            new Course { CourseName = "A", StartDate = baseDate.AddDays(1), EndDate = baseDate.AddDays(2), InstructorUserId = 10 },
            new Course { CourseName = "B", StartDate = baseDate.AddDays(3), EndDate = baseDate.AddDays(4), InstructorUserId = 10 },
            new Course { CourseName = "C", StartDate = baseDate.AddDays(10), EndDate = baseDate.AddDays(11), InstructorUserId = 10 },
            // other instructor
            new Course { CourseName = "X", StartDate = baseDate.AddDays(1), EndDate = baseDate.AddDays(2), InstructorUserId = 99 }
        };

        ctx.Courses.AddRange(courses);
        await ctx.SaveChangesAsync();

        var repo = new CourseRepository(ctx);

        var from = baseDate;
        var to = baseDate.AddDays(5);
        var (items, total) = await repo.SearchAsync(from, to, 10, pageNumber: 1, pageSize: 10);

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
        Assert.DoesNotContain(items, c => c.InstructorUserId != 10);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenParametersMissing()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        ctx.Courses.Add(new Course
        {
            CourseName = "ShouldNotMatch",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            InstructorUserId = 1
        });
        await ctx.SaveChangesAsync();

        var repo = new CourseRepository(ctx);
        var (items, total) = await repo.SearchAsync(null, null, null, 1, 10);

        Assert.Empty(items);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task AddEnrollmentAsync_AddsEnrollment_And_IsStudentEnrolledAsyncReflectsState()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        var course = new Course
        {
            CourseName = "EnrollCourse",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(2),
            InstructorUserId = 20
        };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var repo = new CourseRepository(ctx);

        var enrollment = new CourseEnrollment
        {
            CourseId = course.CourseId,
            UserId = 42,
            EnrollDate = DateTime.UtcNow,
            Course = course
        };

        await repo.AddEnrollmentAsync(enrollment, CancellationToken.None);

        var exists = await repo.IsStudentEnrolledAsync(course.CourseId, 42);
        Assert.True(exists);

        var notExists = await repo.IsStudentEnrolledAsync(course.CourseId, 999);
        Assert.False(notExists);
    }

    [Fact]
    public async Task GetEnrolledCoursesByStudentIdAsync_ReturnsCourses_ForStudentOrderedByStartDate()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);

        var studentId = 77;
        var now = DateTime.UtcNow;

        var c1 = new Course { CourseName = "Late", StartDate = now.AddDays(5), EndDate = now.AddDays(6), InstructorUserId = 1 };
        var c2 = new Course { CourseName = "Early", StartDate = now.AddDays(1), EndDate = now.AddDays(2), InstructorUserId = 1 };
        var c3 = new Course { CourseName = "Middle", StartDate = now.AddDays(3), EndDate = now.AddDays(4), InstructorUserId = 1 };

        ctx.Courses.AddRange(c1, c2, c3);
        await ctx.SaveChangesAsync();

        ctx.CourseEnrollments.AddRange(
            new CourseEnrollment { CourseId = c1.CourseId, UserId = studentId, EnrollDate = now, Course = c1 },
            new CourseEnrollment { CourseId = c2.CourseId, UserId = studentId, EnrollDate = now, Course = c2 },
            new CourseEnrollment { CourseId = c3.CourseId, UserId = 999, EnrollDate = now, Course = c3 } // other student
        );
        await ctx.SaveChangesAsync();

        var repo = new CourseRepository(ctx);
        var enrolled = await repo.GetEnrolledCoursesByStudentIdAsync(studentId);

        Assert.Equal(2, enrolled.Count);
        Assert.Equal("Early", enrolled[0].CourseName);
        Assert.Equal("Late", enrolled[1].CourseName);
    }
}