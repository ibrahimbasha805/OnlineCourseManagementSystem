using UserService.WebApi.Middleware;

namespace UserService.WebApi.Extensions;

public static class ProblemDetailsExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseProblemDetailsExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
    }
}