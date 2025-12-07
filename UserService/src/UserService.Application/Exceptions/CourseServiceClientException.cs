using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Application.Exceptions;

public class CourseServiceClientException: Exception
{
    public int StatusCode { get; }
    public string? ErrorContent { get; }

    public CourseServiceClientException(string message, int statusCode, string? errorContent = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorContent = errorContent;
    }
}
