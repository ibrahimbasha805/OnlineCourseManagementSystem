using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Application.DTOs;

namespace UserService.Application.Abstractions;

public interface IEnrollService
{
    Task<CourseDto> EnrollStudentAsync(EnrollCourseDto dto, string? forwardedAuthorizationHeader);
}
