using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UserService.Application.Abstractions;
using UserService.Application.DTOs;

namespace UserService.WebApi.Controllers;


[ApiController]
[Route("api/v{version:apiVersion}/enroll")]
public class EnrollmentsController : ControllerBase
{
    private readonly ILogger<IEnrollService> _logger;
    private readonly IEnrollService _enrollService;

    public EnrollmentsController(ILogger<IEnrollService> logger,IEnrollService enrollService)
    {
        _logger = logger;
        _enrollService = enrollService;
    }


    [HttpPost]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Enroll([FromBody] EnrollCourseDto dto)
    {
        _logger.LogInformation("Course enrollment request. EnrollCourse:{EnrollCourse}", JsonSerializer.Serialize(dto));
        
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
         await _enrollService.EnrollStudentAsync(dto, authHeader);

        _logger.LogInformation("Course enrolled successfully.EnrollCourse:{EnrollCourse}", JsonSerializer.Serialize(dto));
        return Ok("Course enrolled successfully");
    }
}

