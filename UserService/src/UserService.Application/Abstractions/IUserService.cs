using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Application.DTOs;

namespace UserService.Application.Abstractions;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserDto dto);
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto?> GetByNameAsync(string name);
    Task<List<UserDto>> GetAllAsync();
}
