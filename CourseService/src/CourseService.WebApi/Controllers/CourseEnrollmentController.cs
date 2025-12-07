using CourseService.Application.Abstractions;
using CourseService.Application.DTOs;
using CourseService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CourseService.WebApi.Controllers;

[Route("api/v{version:apiVersion}/courses")]
[ApiController]
public class CourseEnrollmentController : ControllerBase
{
    private readonly ICourseEnrollmentService _enrollService;

    public CourseEnrollmentController(ICourseEnrollmentService enrollService)
    {
        _enrollService = enrollService;
    }


    // POST api/courses/{courseId}/enroll
    [HttpPost("{courseId:int}/enroll")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Enroll(int courseId, [FromBody] EnrollRequestDto request)
    {

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var instructorId))
            return Unauthorized();

        await _enrollService.
         EnrollStudentAsync(courseId, request.StudentId, instructorId);

        return Ok(new { success = true });

    }

    [HttpGet("enrolled")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetEnrolledCoursesForStudent()
    {
        // get caller info from token
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        if (!int.TryParse(idClaim, out var studentId))
            return Unauthorized();       

        // Otherwise (Instructor or other authorized role), allow
        var courses = await _enrollService.GetEnrolledCoursesByStudentAsync(studentId);
        return Ok(courses);
    }
}

