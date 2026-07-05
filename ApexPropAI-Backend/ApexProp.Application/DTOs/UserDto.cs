using ApexProp.Domain.Entities;

namespace ApexProp.Application.DTOs;

/// <summary>
/// UserDto - מה מחזירים עם משתמש (בלי סיסמה!)
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}