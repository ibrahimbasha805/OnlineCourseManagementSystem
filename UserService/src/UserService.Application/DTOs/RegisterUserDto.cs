using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Enum;

namespace UserService.Application.DTOs;

public class RegisterUserDto
{
    public string? Name {  get; set; }
    public string? UserName { get; set; } 
    public string? Password { get; set; } 
    public UserRole Role { get; set; }
}
