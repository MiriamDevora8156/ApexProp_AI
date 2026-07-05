namespace ApexProp.Domain.Entities;

/// <summary>
/// PropertyImage - תמונות של נכס
/// קשור ל-Property ב-One-to-Many
/// </summary>
public class PropertyImage
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int DisplayOrder { get; set; } = 0;
}