using CourseService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseService.Application.Abstractions;

public interface IUserServiceClient
{
    Task<UserDto> GetUserByInstructorNameAsync(string instructorName);
    Task<UserDto> GetUserAsync(int userId);
}
