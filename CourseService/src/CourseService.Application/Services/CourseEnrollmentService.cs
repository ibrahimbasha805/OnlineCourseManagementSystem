using CourseService.Application.Abstractions;
using CourseService.Application.DTOs;
using CourseService.Application.Exceptions;
using CourseService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseService.Application.Services;

// Application/Services/CourseEnrollmentService.cs
public class CourseEnrollmentService: ICourseEnrollmentService
{
    private readonly ICourseRepository _repo;

    public CourseEnrollmentService(ICourseRepository repo)
    {
        _repo = repo;
    }

    
    public async Task<bool> EnrollStudentAsync(int courseId, int studentId, int instructorUserId)
    {
        var course = await _repo.GetByIdAsync(courseId);
        if (course == null)
            throw new BadRequestException("Course not found.");

        if (course.InstructorUserId != instructorUserId)
            throw new BadRequestException("Only the course owner (instructor) can enroll students.");

        var already = await _repo.IsStudentEnrolledAsync(courseId, studentId);
        if (already) new BadRequestException("Student already enrolled.");

        var enrollment = new CourseEnrollment
        {
            CourseId = courseId,
            UserId = studentId,
            EnrollDate = DateTime.UtcNow
        };

        await _repo.AddEnrollmentAsync(enrollment);
        
        return true;
    }

    public async Task<List<CourseDto>> GetEnrolledCoursesByStudentAsync(int studentId)
    {
        var courses = await _repo.GetEnrolledCoursesByStudentIdAsync(studentId);
        return courses.Select(c => new CourseDto
        {
            CourseName = c.CourseName,
            StartDate = c.StartDate,
            EndDate = c.EndDate
        }).ToList();
    }
}

