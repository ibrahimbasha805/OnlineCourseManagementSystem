using Asp.Versioning;
using CourseService.Application.Abstractions;
using CourseService.Application.Services;
using CourseService.Application.Validation;
using CourseService.Infrastructure.Persistence;
using CourseService.Infrastructure.Repositories;
using CourseService.Infrastructure.UserService;
using CourseService.WebApi;
using CourseService.WebApi.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

//Read Serilog configuration from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssembly(typeof(CreateCourseDtoValidator).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }});
    c.DocumentFilter<ServersDocumentFilter>();
});

builder.Services.AddDbContext<CourseDbContext>(options =>
    options.UseInMemoryDatabase("CourseServiceDb"));


builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService.Application.Services.CourseService>();
builder.Services.AddScoped<ICourseEnrollmentService, CourseEnrollmentService>();
builder.Services.AddScoped<IUserServiceClient, UserServiceClient>();

builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["UserService:BaseUrl"] ?? "https://localhost:5001");
});



builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0); // If no version specified, use v1
    options.AssumeDefaultVersionWhenUnspecified = true; // Apply default version automatically
    options.ReportApiVersions = true; // Shows available versions in Response Headers
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Formats group name: v1, v2, v3
    options.SubstituteApiVersionInUrl = true; // Replace version placeholder in routes
});

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("JWT failed: " + context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // In production, add KnownProxies or KnownNetworks to restrict which proxies are trusted
    // options.KnownProxies.Add(IPAddress.Parse("172.18.0.3"));
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseProblemDetailsExceptionHandler();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.UseAuthentication();   
app.UseAuthorization();
//app.UseExceptionHandler();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";           // keep swagger at /.../swagger
    c.SwaggerEndpoint("/courses/swagger/v1/swagger.json", "CourseService V1"); // <<-- relative path
});


app.UseHttpsRedirection();

app.MapControllers();

app.Run();
