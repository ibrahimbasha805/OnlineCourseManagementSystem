using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseService.Application.DTOs;

public class AllCoursesDto
{
    public int CourseId { get; set; }
    public string? CourseName { get; set; }   
    public int InstructorUserId { get; set; }
}
