using UserService.Domain.Enum;

namespace UserService.Domain.Entities;

public class User
{
    public int UserId { get; private set; }      
    public string? Name { get; private set; }
    public string? UserName { get; private set; }
    public string? Password { get; private set; }
    public UserRole Role { get; private set; }
        
    public User(string? name, string? userName, string? password, UserRole role)
    {
        Name = name;    
        UserName = userName;
        Password = password;
        Role = role;
    }     
}
