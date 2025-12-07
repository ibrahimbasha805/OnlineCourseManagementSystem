using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseService.Application.DTOs;

public record UpdateCourseDto
{
    public string? CourseName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int InstructorId { get; set; }
}
