using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Net;
using System.Text.Json;
using UserService.Application.Exceptions;

namespace UserService.WebApi.Middleware;

public class ProblemDetailsExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsExceptionMiddleware> _logger;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public ProblemDetailsExceptionMiddleware(
        RequestDelegate next,
        ILogger<ProblemDetailsExceptionMiddleware> logger,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _next = next;
        _logger = logger;
        _problemDetailsFactory = problemDetailsFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadRequestException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            await WriteValidationProblemAsync(context, ex);
        }
        catch (CourseServiceClientException ex)
        {
            _logger.LogError(ex, "API client error calling CourseService");
            await WriteApiClientProblemAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteUnhandledProblemAsync(context, ex);
        }
    }

    private Task WriteValidationProblemAsync(HttpContext context, BadRequestException ex)
    {
        var status = (int)HttpStatusCode.BadRequest;

        var pd = _problemDetailsFactory.CreateProblemDetails(
            context,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            detail: ex.Message,
            instance: context.Request.Path
        );

        // Optionally include trace id
        pd.Extensions["traceId"] = context.TraceIdentifier;

        return WriteProblemResponseAsync(context, pd, status);
    }

    private Task WriteApiClientProblemAsync(HttpContext context, CourseServiceClientException ex)
    {

        var status = ex.StatusCode >= 400 && ex.StatusCode < 600
            ? ex.StatusCode
            : (int)HttpStatusCode.BadGateway; // remote service problem

        var pd = _problemDetailsFactory.CreateProblemDetails(
            context,
            statusCode: status,
            title: "Upstream API error",
            detail: ex.Message,
            instance: context.Request.Path
        );

        // Add remote body (if any) in extensions but be careful with PII
        if (!string.IsNullOrWhiteSpace(ex.ErrorContent))
        {
            pd.Extensions["remoteErrorContent"] = ex.ErrorContent;
        }

        return WriteProblemResponseAsync(context, pd, status);
    }

    private Task WriteUnhandledProblemAsync(HttpContext context, Exception ex)
    {
        var status = (int)HttpStatusCode.InternalServerError;
        var pd = _problemDetailsFactory.CreateProblemDetails(
            context,
            statusCode: status,
            title: "An unexpected error occurred.",
            detail: "An unexpected error occurred while processing your request.",
            instance: context.Request.Path
        );

        // Optionally include exception message in development only:
        pd.Extensions["traceId"] = context.TraceIdentifier;

        return WriteProblemResponseAsync(context, pd, status);
    }

    private Task WriteProblemResponseAsync(HttpContext context, ProblemDetails pd, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Use System.Text.Json to serialize ProblemDetails
        return context.Response.WriteAsJsonAsync(pd, options);
    }
}
