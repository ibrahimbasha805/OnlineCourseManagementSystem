using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using UserService.Application.Abstractions;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;

namespace UserService.Application.Services;

public class EnrollService : IEnrollService
{
    private readonly ILogger<EnrollService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public EnrollService(ILogger<EnrollService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<CourseDto> EnrollStudentAsync(EnrollCourseDto dto, string? forwardedAuthorizationHeader)
    {
        var baseUrl = _configuration["CourseService:BaseUrl"] ?? "https://localhost:7001";
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);

        if (!string.IsNullOrWhiteSpace(forwardedAuthorizationHeader))
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", forwardedAuthorizationHeader);

        var payload = new { StudentId = dto.UserId };
        var url = $"/api/v1/courses/{dto.CourseId}/enroll";

        var resp = await client.PostAsJsonAsync(url, payload);

        var body = await resp.Content.ReadAsStringAsync();
        
        if (!resp.IsSuccessStatusCode)
        {
            var message = $"CourseService failed with statusCode:{(int)resp.StatusCode}, ReasonPhrase:{resp.ReasonPhrase}, Url:{url}";
            var exception = new CourseServiceClientException(
                message: message,
                statusCode: (int)resp.StatusCode,
                errorContent: body);
            _logger.LogError(exception, message);

            throw exception;

        }

        _logger.LogInformation("CourseService Api call successfull, Url:{Url}", url);


        var course = JsonSerializer.Deserialize<CourseDto>(body);

        return course!;
    }
}
