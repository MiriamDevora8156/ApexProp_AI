namespace ApexProp.Domain.Entities;

public class Property
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // תיאור מילולי של הנכס
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal Price { get; set; }
    public int Rooms { get; set; }
    public double AreaSqm { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // --- נתוני AI מתקדמים ---
    public double AIScore { get; set; }
    public decimal? EstimatedValue { get; set; } // חיזוי המחיר האמיתי על ידי המודל
    public string? AIAnalysisNotes { get; set; } // תובנות של ה-AI מנוסחות במלל

    // --- קשרים (Navigation Properties) ---
    public List<PropertyImage> Images { get; set; } = new(); // תמונות
    public List<PriceHistory> PriceHistories { get; set; } = new();
    public List<Location> NearbyLocations { get; set; } = new();
    public List<User> SavedByUsers { get; set; } = new(); // קשר הפוך למועדפים
}