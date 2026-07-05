namespace ApexProp.Application.DTOs;

/// <summary>
/// CreateUserDto - מה אנגולר שולח בהרשמה
/// </summary>
public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}