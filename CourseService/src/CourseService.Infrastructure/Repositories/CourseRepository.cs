using CourseService.Application.Abstractions;
using CourseService.Domain.Entities;
using CourseService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace CourseService.Infrastructure.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly CourseDbContext _dbContext;

    public CourseRepository(CourseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddEnrollmentAsync(CourseEnrollment enrollment, CancellationToken cancellationToken = default)
    {
        await _dbContext.CourseEnrollments.AddAsync(enrollment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsStudentEnrolledAsync(int courseId, int userId)
    {
        return await _dbContext.CourseEnrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == userId);
    }

    public async Task<List<Course>> GetEnrolledCoursesByStudentIdAsync(int studentId)
    {        
        return await _dbContext.Courses
            .Where(c => c.Enrollments.Any(e => e.UserId == studentId))
            .OrderBy(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Courses
            .FirstOrDefaultAsync(c => c.CourseId == id, cancellationToken);
    }

    public async Task AddAsync(Course course, CancellationToken cancellationToken = default)
    {
        await _dbContext.Courses.AddAsync(course, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Course course, CancellationToken cancellationToken = default)
    {
        _dbContext.Courses.Update(course);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await _dbContext.Courses.FindAsync(new object[] { id }, cancellationToken);
        if (course is null)
            return;

        _dbContext.Courses.Remove(course);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Course>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Courses
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }


    public async Task<(List<Course> Items, int TotalCount)> SearchAsync(
        DateTime? fromDate,
        DateTime? toDate,
        int? instructorId,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.Courses.AsQueryable();


        if (fromDate.HasValue && toDate.HasValue && instructorId.HasValue)
        {
            query = query.Where(c =>
                (c.StartDate >= fromDate.Value
                && c.EndDate <= toDate.Value) && c.InstructorUserId == instructorId!.Value);

        }
        else
        {
            return new(new List<Course>(), 0);
        }


       var totalCount = await query.CountAsync();

        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var skip = (pageNumber - 1) * pageSize;

        var items = await query
            .OrderBy(c => c.StartDate)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

}
