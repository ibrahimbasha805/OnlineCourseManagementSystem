using CourseService.Application.Abstractions;
using CourseService.Application.DTOs;
using CourseService.Application.Exceptions;
using CourseService.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace CourseService.Application.Services;

public class CourseService : ICourseService
{
    private readonly ILogger<CourseService> _logger;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserServiceClient _UserServiceClient;

    public CourseService(ILogger<CourseService> logger,
                         ICourseRepository courseRepository,
                         IUserServiceClient UserServiceClient
                        )
    {
        _logger = logger;
        _courseRepository = courseRepository;
        _UserServiceClient = UserServiceClient;
    }

    public async Task<int> CreateCourseAsync(CreateCourseDto dto,CancellationToken cancellationToken = default)
    {

        var isInstructor = await IsInstructor(dto.InstructorId);
        if (!isInstructor)
        {
            throw new BadRequestException($" InstructorId {dto.InstructorId} is not a instructor.");
        }

        var course = new Course
        {
            CourseName = dto.CourseName,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            InstructorUserId = dto.InstructorId            
        };

        await _courseRepository.AddAsync(course, cancellationToken);

        
        return course.CourseId;
    }

    public async Task<List<AllCoursesDto>> GetAllCourseAsync(CancellationToken cancellationToken = default)
    {
        var courses = await _courseRepository.GetAllAsync(cancellationToken);

        var coursesDto = courses.Select(x => new AllCoursesDto()
        {
            CourseId = x.CourseId,
            CourseName = x.CourseName,
            InstructorUserId = x.InstructorUserId

        }).ToList();

        return coursesDto;

    }


    public async Task<CourseDto?> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdAsync(id, cancellationToken);
        if (course is null)
            return null;

        return new CourseDto
        {            
            CourseName = course.CourseName,
            StartDate = course.StartDate,
            EndDate = course.EndDate,            
        };
    }


    public async Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var isInstructor = await IsInstructor(dto.InstructorId);
        if (!isInstructor)
        {
            throw new BadRequestException($" InstructorId {dto.InstructorId} is not a instructor.");
        }

        var course = await _courseRepository.GetByIdAsync(id, cancellationToken);
        if (course is null)
        {
            return false;
        }

        course.CourseName = dto.CourseName;
        course.StartDate = dto.StartDate;
        course.EndDate = dto.EndDate;
        course.InstructorUserId = dto.InstructorId;

        await _courseRepository.UpdateAsync(course, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdAsync(id, cancellationToken);
        if (course is null)
        {
            return false;
        }

        await _courseRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    public async Task<PagedResult<SearchCourseDto>> SearchAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? instructorName,
        int pageNumber,
        int pageSize)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var instructorUserId = await GetInstructorUserId(instructorName);

        var (items, totalCount) = await _courseRepository.SearchAsync(
            fromDate, toDate, instructorUserId, pageNumber, pageSize);

        //var dtoItems = items.Select(MapToDto).ToList();

        List<SearchCourseDto> courseDtos = new();

        foreach (var c in items)
        {
            courseDtos.Add(new SearchCourseDto()
            {
                CourseName = c.CourseName,
                InstructorName = instructorName,
                StartDate = c.StartDate,
                EndDate = c.EndDate,

            });
        }

        return new PagedResult<SearchCourseDto>
        {
            Items = courseDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task<int> GetInstructorUserId(string? instructorName)
    {   
        var users = await _UserServiceClient.GetUserByInstructorNameAsync(instructorName!);

        return users!.userId;
    }

    private async Task<bool> IsInstructor(int userId)
    {   

        var users = await _UserServiceClient.GetUserAsync(userId);

        return users?.roleName == "Instructor" ? true : false;
        
    }    
}
