using Microsoft.AspNetCore.Mvc;
using UserService.Application.Abstractions;

namespace UserService.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetById(int userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();
        return Ok(user); 
    }

    [HttpGet("search")]
    public async Task<IActionResult> GetByName([FromQuery] string name)
    {
        var user = await _userService.GetByNameAsync(name);
        if (user == null) return NotFound();
        return Ok(user); 
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }
}
