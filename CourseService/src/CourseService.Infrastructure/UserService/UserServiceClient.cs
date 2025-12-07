using CourseService.Application.Abstractions;
using CourseService.Application.DTOs;
using CourseService.Application.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CourseService.Infrastructure.UserService;

public class UserServiceClient: IUserServiceClient
{
    private readonly ILogger<UserServiceClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    public UserServiceClient(ILogger<UserServiceClient> logger ,IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory; 
    }
    public async Task<UserDto> GetUserByInstructorNameAsync(string? instructorName)
    {
        var client = _httpClientFactory.CreateClient("UserService");
        var url = $"/api/v1/users/search?name={WebUtility.UrlEncode(instructorName)}";
        
        var resp = await client.GetAsync(url);
               
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            var message = $"UserService failed with statusCode:{(int)resp.StatusCode}, ReasonPhrase:{resp.ReasonPhrase}, Url:{url}";
            var exception= new UserServiceClientException(
                message: message,
                statusCode: (int)resp.StatusCode,
                errorContent: body);
            _logger.LogError(exception, message);

            throw exception;


        }

        _logger.LogInformation("UserService Api call successfull, Url:{Url}",url);

        var users = JsonSerializer.Deserialize<UserDto>(body);

        return users!;

    }

    public async Task<UserDto> GetUserAsync(int userId)
    {
        var client = _httpClientFactory.CreateClient("UserService");
        var url = $"/api/v1/users/{userId}";
        HttpResponseMessage resp;

        resp = await client.GetAsync(url);

        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            var message = $"UserService GET failed with {(int)resp.StatusCode} {resp.ReasonPhrase}, Url:{url}";
            var exception = new UserServiceClientException(
                message: message,
                statusCode: (int)resp.StatusCode,
                errorContent: body);

            _logger.LogError(exception, message);

            throw exception;
        }

        var users = JsonSerializer.Deserialize<UserDto>(body);

        _logger.LogInformation("UserService Api call successfull. Url:{Url}",url);

        return users!;
    }

}
