using Microsoft.AspNetCore.Mvc;
using UserService.Application.Abstractions;
using UserService.Application.DTOs;

namespace UserService.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IUserService _userService;

    public AuthController(ILogger<AuthController> logger,IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    [HttpPost("register")]
    [ProducesResponseType(200, Type=typeof(UserDto))]

    public async Task<IActionResult> Register(RegisterUserDto dto)
    {
        var user =await _userService.RegisterAsync(dto);

        _logger.LogInformation("User {User} register successfull.", dto.Name);
        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var result = await _userService.LoginAsync(dto);
        if (result == null)
        {
            return Unauthorized("Invalid username or password.");
        }

        _logger.LogInformation("User {username} login successfull.", dto.UserName);

        return Ok(result); 
    }
}
