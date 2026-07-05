using ApexProp.Domain.Models;

namespace ApexProp.Domain.Interfaces;

/// <summary>
/// IExternalLocationService - חוזה לשירותים חיצוניים של מיקום
/// אפשר להחליף בין OpenStreetMap, Google Maps וכו' בלי לשנות AI
/// </summary>
public interface IExternalLocationService
{
    /// <summary>
    /// קבל רשימת נקודות עניין (POI) סביב קואורדינטה מסוימת
    /// </summary>
    Task<IEnumerable<ExternalLocationResult>> GetNearbyLocationsAsync(
        double latitude,
        double longitude,
        double radiusInMeters);

    /// <summary>
    /// בדוק אם השירות זמין (בדיקת בריאות)
    /// </summary>
    Task<bool> IsAvailableAsync();
}