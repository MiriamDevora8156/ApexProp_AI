namespace ApexProp.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // --- שדות פרופיל חדשים ---
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; } // קישור לתמונת פרופיל
    public string Role { get; set; } = "User"; // הרשאה (למקרה שתרצי Admin בעתיד)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // קשר רבים-לרבים: המועדפים של המשתמש
    public List<Property> SavedProperties { get; set; } = new();

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}