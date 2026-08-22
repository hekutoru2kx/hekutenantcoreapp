namespace Hekutenantcoreapp.Application.DTOs;

public class CreateUserDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Role { get; set; }
}