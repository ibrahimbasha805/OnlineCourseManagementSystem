# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first (this lets Docker cache restore)
COPY ["src/UserService.WebApi/UserService.WebApi.csproj", "src/UserService.WebApi/"]
COPY ["src/UserService.Application/UserService.Application.csproj", "src/UserService.Application/"]
COPY ["src/UserService.Domain/UserService.Domain.csproj", "src/UserService.Domain/"]
COPY ["src/UserService.Infrastructure/UserService.Infrastructure.csproj", "src/UserService.Infrastructure/"]

# Restore (cached if csproj didn't change)
RUN dotnet restore "src/UserService.WebApi/UserService.WebApi.csproj"

# Copy the rest of the source code
COPY . .

# Publish
RUN dotnet publish "src/UserService.WebApi/UserService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "UserService.WebApi.dll"]
