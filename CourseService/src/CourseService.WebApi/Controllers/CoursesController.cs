using CourseService.Application.Abstractions;
using CourseService.Application.DTOs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Data;
using System.Security.Claims;

namespace CourseService.WebApi.Controllers;


[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ILogger<CoursesController> _logger;
    private readonly IValidator<CreateCourseDto> _createCourseValidator;
    private readonly IValidator<UpdateCourseDto> _updateCourseValidator;
    private readonly ICourseService _courseService;

    public CoursesController(ILogger<CoursesController> logger,
         IValidator<CreateCourseDto> createCourseValidator,
         IValidator<UpdateCourseDto> updateCourseValidator,
         ICourseService courseService)
    {
        _logger = logger;
        _createCourseValidator = createCourseValidator;
        _updateCourseValidator = updateCourseValidator;
        _courseService = courseService;

    }

       
    [Authorize(Roles = "Instructor")]
    [HttpPost]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseDto request,
        CancellationToken cancellationToken)
    {   
        var modelState= await ValidateCreateCourse(request, cancellationToken);
        if (modelState!=null)
        {
            return ValidationProblem(ModelState); 
        }

       // var instructorUserid = Convert.ToInt16(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
       // var instructorName = User.FindFirst(ClaimTypes.Name)?.Value;

        var newCourseId = await _courseService.CreateCourseAsync(request, cancellationToken);


        return CreatedAtAction(
            nameof(GetCourseById),
            new { id = newCourseId },
            new { id = newCourseId });
    }



    [Authorize(Roles = "Instructor")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourseById(
        int id,
        CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCourseByIdAsync(id, cancellationToken);

        if (course is null)
            return NotFound();

        return Ok(course);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        [FromBody] UpdateCourseDto request,
        CancellationToken cancellationToken)
    {
        var modelState = await ValidateUpdateCourse(request, cancellationToken);
        if (modelState != null)
        {
            return ValidationProblem(ModelState);
        }

        var instructorUserid = Convert.ToInt16(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var updated = await _courseService.UpdateCourseAsync(id, request,  cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    [Authorize(Roles = "Instructor")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _courseService.DeleteCourseAsync(id, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    
    [HttpGet("search")]
    [AllowAnonymous] 
    public async Task<ActionResult<PagedResult<CourseDto>>> Search(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? instructorName,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _courseService.SearchAsync(
            fromDate, toDate, instructorName, pageNumber, pageSize);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCourses(
        CancellationToken cancellationToken)
    {
        var courses = await _courseService.GetAllCourseAsync(cancellationToken);
        return Ok(courses);
    }

    private async Task<ModelStateDictionary?> ValidateCreateCourse(CreateCourseDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _createCourseValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return GetModelState(validationResult);
        }

        return null;
    }

    private async Task<ModelStateDictionary?> ValidateUpdateCourse(UpdateCourseDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _updateCourseValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return GetModelState(validationResult);
        }

        return null;
    }

    private ModelStateDictionary GetModelState(ValidationResult validationResult)
    {
        foreach (var failure in validationResult.Errors)
        {
            ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
        }

        return ModelState;
    }
}

