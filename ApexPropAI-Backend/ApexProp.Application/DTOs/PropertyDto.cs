using ApexProp.Domain.Entities;

namespace ApexProp.Application.DTOs;

public class PropertyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal Price { get; set; }
    public int Rooms { get; set; }
    public double AreaSqm { get; set; }
    public DateTime CreatedAt { get; set; }

    // שדות AI
    public double AIScore { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? AIAnalysisNotes { get; set; }

    // רשימת תמונות (רק הכתובות שלהן)
    public List<string> Images { get; set; } = new();

    // Helper - חישוב מחיר למ"ר
    public decimal PricePerSqm => AreaSqm > 0 ? Price / (decimal)AreaSqm : 0;
    public List<LocationDto> NearbyLocations { get; set; } = new();

}