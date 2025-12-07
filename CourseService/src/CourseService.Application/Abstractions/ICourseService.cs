using CourseService.Application.DTOs;

namespace CourseService.Application.Abstractions;

public interface ICourseService
{
    Task<int> CreateCourseAsync(CreateCourseDto dto,CancellationToken cancellationToken = default);
    Task<CourseDto?> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<SearchCourseDto>> SearchAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? instructorName,
        int pageNumber,
        int pageSize);

    Task<List<AllCoursesDto>> GetAllCourseAsync(CancellationToken cancellationToken = default);

}
