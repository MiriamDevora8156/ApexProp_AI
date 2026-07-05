namespace ApexProp.Domain.Models;

/// <summary>
/// ExternalLocationResult - תוצאה מ-API חיצוני של מיקומים
/// משמש לחזרה מ-OpenStreetMap Service
/// </summary>
public record ExternalLocationResult(
    string Name,
    string Type,
    double DistanceInMeters,
    double Latitude,
    double Longitude,
    double? Rating = null);