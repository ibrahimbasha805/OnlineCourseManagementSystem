using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Application.Exceptions;

public class BadRequestException : Exception
{   

    public BadRequestException(string message, string? errorContent = null)
        : base(message)
    {
        
    }
}