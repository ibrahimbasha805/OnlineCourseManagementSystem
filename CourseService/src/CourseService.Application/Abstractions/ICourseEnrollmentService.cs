using CourseService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseService.Application.Abstractions;

public interface ICourseEnrollmentService
{
    Task<bool> EnrollStudentAsync(int courseId, int studentId, int instructorUserId);
    Task<List<CourseDto>> GetEnrolledCoursesByStudentAsync(int studentId);
}
