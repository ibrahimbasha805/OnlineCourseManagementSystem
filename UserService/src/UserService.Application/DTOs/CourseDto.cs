using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Application.DTOs;

public class CourseDto
{
    public string? CourseName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

}