# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first (this lets Docker cache restore)
COPY ["src/CourseService.WebApi/CourseService.WebApi.csproj", "src/CourseService.WebApi/"]
COPY ["src/CourseService.Application/CourseService.Application.csproj", "src/CourseService.Application/"]
COPY ["src/CourseService.Domain/CourseService.Domain.csproj", "src/CourseService.Domain/"]
COPY ["src/CourseService.Infrastructure/CourseService.Infrastructure.csproj", "src/CourseService.Infrastructure/"]

# Restore (cached if csproj didn't change)
RUN dotnet restore "src/CourseService.WebApi/CourseService.WebApi.csproj"

# Copy the rest of the source code
COPY . .

# Publish
RUN dotnet publish "src/CourseService.WebApi/CourseService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CourseService.WebApi.dll"]
