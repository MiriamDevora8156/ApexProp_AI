namespace ApexProp.Application.DTOs;

/// <summary>
/// LoginResponseDto - מה מחזירים אחרי התחברות הצליחה
/// </summary>
public class LoginResponseDto
{
    public UserDto User { get; set; } = null!;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}