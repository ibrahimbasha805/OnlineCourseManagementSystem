using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseService.Application.Exceptions;

public class UserServiceClientException : Exception
{
    public int StatusCode { get; }
    public string? ErrorContent { get; }

    public UserServiceClientException(string message, int statusCode, string? errorContent = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorContent = errorContent;
    }
}

