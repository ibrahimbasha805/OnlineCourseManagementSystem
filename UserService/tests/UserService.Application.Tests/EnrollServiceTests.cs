using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Application.Abstractions;
using UserService.Application.DTOs;
using UserService.Application.Exceptions;
using UserService.Application.Services;
using Xunit;

namespace UserService.Application.Tests;

public class EnrollServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<EnrollService>> _loggerMock;
    private readonly EnrollService _sut;

    public EnrollServiceTests()
    {
        _fixture = new Fixture();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<EnrollService>>();

        _sut = new EnrollService(_loggerMock.Object, _httpClientFactoryMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task EnrollStudentAsync_OnSuccess_ReturnsCourseDto()
    {
        // Arrange
        var dto = _fixture.Create<EnrollCourseDto>();
        var expectedCourse = new CourseDto
        {
            CourseName = "Test Course",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10)
        };

        var body = JsonSerializer.Serialize(expectedCourse);
        var handler = new CaptureRequestHandler((req, token) =>
        {
            // basic sanity assertions inside handler (optional)
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.AbsolutePath.Should().EndWith($"/api/v1/courses/{dto.CourseId}/enroll");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
        });

        var client = new HttpClient(handler);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        // Act
        var result = await _sut.EnrollStudentAsync(dto, forwardedAuthorizationHeader: null);

        // Assert
        result.Should().NotBeNull();
        result!.CourseName.Should().Be(expectedCourse.CourseName);
        result.StartDate.Should().Be(expectedCourse.StartDate);
        result.EndDate.Should().Be(expectedCourse.EndDate);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(500)]
    public async Task EnrollStudentAsync_OnNonSuccess_ThrowsCourseServiceClientException(int statusCode)
    {
        // Arrange
        var dto = _fixture.Create<EnrollCourseDto>();
        var errorBody = "{\"error\":\"bad\"}";
        var handler = new CaptureRequestHandler((req, token) =>
        {
            var resp = new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                ReasonPhrase = "Bad",
                Content = new StringContent(errorBody)
            };
            return Task.FromResult(resp);
        });

        var client = new HttpClient(handler);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        // Act
        var act = async () => await _sut.EnrollStudentAsync(dto, forwardedAuthorizationHeader: null);

        // Assert
        var ex = await Assert.ThrowsAsync<CourseServiceClientException>(act);
        ex.StatusCode.Should().Be(statusCode);
        ex.ErrorContent.Should().Be(errorBody);
        ex.Message.Should().Contain($"/api/v1/courses/{dto.CourseId}/enroll");
    }

    [Fact]
    public async Task EnrollStudentAsync_ForwardsAuthorizationHeader_WhenProvided()
    {
        // Arrange
        var dto = _fixture.Create<EnrollCourseDto>();
        var expectedCourse = new CourseDto
        {
            CourseName = "C",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var body = JsonSerializer.Serialize(expectedCourse);
        HttpRequestMessage? capturedRequest = null;

        var handler = new CaptureRequestHandler((req, token) =>
        {
            capturedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
        });

        var client = new HttpClient(handler);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var forwarded = "Bearer sometoken";

        // Act
        var result = await _sut.EnrollStudentAsync(dto, forwardedAuthorizationHeader: forwarded);

        // Assert
        result.Should().NotBeNull();
        capturedRequest.Should().NotBeNull();
        // Authorization header was added via TryAddWithoutValidation("Authorization", forwardedHeader)
        capturedRequest!.Headers.TryGetValues("Authorization", out var values).Should().BeTrue();
        values!.Should().ContainSingle().And.Contain(forwarded);
    }

    // Helper DelegatingHandler to capture the outgoing request and return a custom response
    private class CaptureRequestHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public CaptureRequestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _responder(request, cancellationToken);
    }
}