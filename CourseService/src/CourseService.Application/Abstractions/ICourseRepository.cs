using CourseService.Domain.Entities;

namespace CourseService.Application.Abstractions;

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Course course, CancellationToken cancellationToken = default);
    Task UpdateAsync(Course course, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(List<Course> Items, int TotalCount)> SearchAsync(
        DateTime? fromDate,
        DateTime? toDate,
        int? instructorId,
        int pageNumber,
        int pageSize);

    Task AddEnrollmentAsync(CourseEnrollment enrollment, CancellationToken cancellationToken = default);

    Task<bool> IsStudentEnrolledAsync(int courseId, int userId);

    Task<List<Course>> GetEnrolledCoursesByStudentIdAsync(int studentId);
}

