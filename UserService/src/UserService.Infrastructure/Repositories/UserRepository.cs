using Microsoft.EntityFrameworkCore;
using UserService.Application.Abstractions;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _dbContext;

    public UserRepository(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetByUserNameAsync(string? userName)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u =>
                u.UserName!.ToLower() == userName!.ToLower());
    }

    public async Task<User?> GetByNameAsync(string? name)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u =>
                u.Name!.ToLower() == name!.ToLower());
    }

    public async Task<List<User>> GetAllUsers()
    {
        return await _dbContext.Users.ToListAsync();
    }

    public async Task<int> AddAsync(User user)
    {
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return user.UserId;
    }

    
}
