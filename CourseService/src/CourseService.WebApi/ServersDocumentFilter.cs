using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CourseService.WebApi;

public class ServersDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // The path prefix Nginx exposes for this service:
        // For UserService use "/users"
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = "/courses" }
        };
    }
}