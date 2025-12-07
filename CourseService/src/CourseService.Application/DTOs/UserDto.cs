using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseService.Application.DTOs;

public class UserDto
{
    public int userId { get; set; }
    public string? name { get; set; }
    public string? roleName { get; set; }
}
