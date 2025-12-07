using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetByUserNameAsync(string? userName);
    Task<int> AddAsync(User user);

    Task<User?> GetByNameAsync(string? name);
}
