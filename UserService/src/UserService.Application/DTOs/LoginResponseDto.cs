using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Enum;

namespace UserService.Application.DTOs;

public class LoginResponseDto
{
    public string? Token { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; } 
    public string? Role { get; set; }
}
