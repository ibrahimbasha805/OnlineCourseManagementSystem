using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Application.Abstractions;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Domain.Entities;

namespace UserService.Application.Services;

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;    
    public UserService(ILogger<UserService> logger,IConfiguration configuration, IUserRepository userRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _userRepository = userRepository;
    }

    public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
    {
        var existing = await _userRepository.GetByUserNameAsync(dto.UserName);
        if (existing != null)
            throw new BadRequestException("User already exists.");

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User(dto.Name, dto.UserName, hashedPassword, dto.Role);
        var userid = await _userRepository.AddAsync(user);

        return new UserDto() { UserId = userid, Name = dto.Name, RoleName = dto.Role.ToString() };
        
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userRepository.GetByUserNameAsync(dto.UserName);
        if (user == null)
            return null;

        bool passwordMatches = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

        if (passwordMatches)
        {
            _logger.LogInformation("Login Success!");
        }
        else
        {
            return null;
        }

        var token = GenerateJwtToken(user);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.UserId,
            UserName = user.UserName,
            Role = user.Role.ToString(),
        };
    }

    public async Task<UserDto?> GetByIdAsync(int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null) return null;

        return new UserDto
        {
            UserId = user.UserId,
            Name= user.Name,            
            RoleName = user.Role.ToString(),
        };
    }


    public async Task<UserDto?> GetByNameAsync(string name)
    {
        var user = await _userRepository.GetByNameAsync(name);
        if (user == null) return null;

        return new UserDto
        {
            UserId = user.UserId,
            Name = user.Name,
            //UserName = user.UserName,
            RoleName = user.Role.ToString(),
        };
    }

    private string GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Name!),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllUsers();
        var userDtos = users.Select(user => new UserDto
        {
            UserId = user.UserId,
            Name = user.Name,
            RoleName = user.Role.ToString(),
        }).ToList();
        return userDtos;
    }
}
