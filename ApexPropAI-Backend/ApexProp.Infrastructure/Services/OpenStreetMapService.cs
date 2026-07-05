using Microsoft.Extensions.Logging;
using ApexProp.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApexProp.Domain.Models;


namespace ApexProp.Infrastructure.Services;

/// <summary>
/// OpenStreetMapService - שירות חינמי לשליפת נתונים גיאוגרפיים
/// משתמש בـ Overpass API שהוא חלק מ-OpenStreetMap
/// </summary>
public class OpenStreetMapService : IExternalLocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenStreetMapService> _logger;
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";
    private const int RequestTimeoutSeconds = 30;

    // מיקומי עניין שמעניינים אותנו
    private static readonly Dictionary<string, string[]> PoiCategories = new()
    {
        { "transportation", new[] { "bus_station", "train_station", "tram_stop", "parking", "taxi" } },
        { "education", new[] { "school", "kindergarten", "university", "college", "library" } },
        { "leisure", new[] { "park", "playground", "swimming_pool", "sports_centre", "cinema", "theatre" } },
        { "healthcare", new[] { "hospital", "clinic", "pharmacy", "dentist", "veterinary" } },
        { "shopping", new[] { "supermarket", "shop", "market", "mall", "bakery" } },
        { "dining", new[] { "restaurant", "cafe", "bar", "fast_food" } }
    };

    public OpenStreetMapService(HttpClient httpClient, ILogger<OpenStreetMapService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // הגדר את ה-User-Agent (OpenStreetMap דורש זאת)
        _httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "ApexProp-RealEstateAI/1.0 (+https://ApexProp.com)");

        _httpClient.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
    }

    /// <summary>
    /// קבל רשימת נקודות עניין סביב קואורדינטה
    /// </summary>
    public async Task<IEnumerable<ExternalLocationResult>> GetNearbyLocationsAsync(
        double latitude,
        double longitude,
        double radiusInMeters)
    {
        try
        {
            // בדוק תקינות קוואורדינטות
            if (!IsValidCoordinate(latitude, longitude))
            {
                _logger.LogWarning("Invalid coordinates: lat={Lat}, lng={Lng}", latitude, longitude);
                return new List<ExternalLocationResult>();
            }

            // בנה את שאילתת Overpass API
            string query = BuildOverpassQuery(latitude, longitude, radiusInMeters);

            _logger.LogInformation("Querying OpenStreetMap for POIs around ({Lat}, {Lng}) with radius {Radius}m",
                latitude, longitude, radiusInMeters);

            // שלח בקשה ל-API
            var response = await _httpClient.PostAsync(
                OverpassUrl,
                new StringContent(query, System.Text.Encoding.UTF8, "text/plain"));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenStreetMap API returned status {Status}", response.StatusCode);
                return new List<ExternalLocationResult>();
            }

            // פענח את התשובה
            var content = await response.Content.ReadAsStringAsync();
            var results = ParseOsmResponse(content, latitude, longitude);

            _logger.LogInformation("Found {Count} nearby locations", results.Count());
            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while querying OpenStreetMap");
            return new List<ExternalLocationResult>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while processing OpenStreetMap response");
            return new List<ExternalLocationResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while querying OpenStreetMap");
            return new List<ExternalLocationResult>();
        }
    }

    /// <summary>
    /// בדוק אם השירות זמין
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // נסה בקשה פשוטה
            var response = await _httpClient.PostAsync(
                OverpassUrl,
                new StringContent("", System.Text.Encoding.UTF8, "text/plain"));

            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest;
        }
        catch
        {
            return false;
        }
    }

    // ============= PRIVATE METHODS =============

    /// <summary>
    /// בנה שאילתת Overpass API
    /// זו שפה שאמורה ל-OpenStreetMap
    /// </summary>
    private string BuildOverpassQuery(double lat, double lng, double radiusInMeters)
    {
        // בנה רשימה של כל האמנויות שמעניינות אותנו
        var amenities = new List<string>();
        foreach (var category in PoiCategories.Values)
        {
            amenities.AddRange(category);
        }

        // בנה את השאילתה
        var amenityFilter = string.Join("|", amenities);
        var query = $@"[out:json][timeout:30];
(
  node(around:{radiusInMeters},{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)})[amenity~""{amenityFilter}""];
  way(around:{radiusInMeters},{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)})[amenity~""{amenityFilter}""];
);
out center;";

        return query;
    }

    /// <summary>
    /// פענח את תשובת OpenStreetMap JSON
    /// </summary>
    private List<ExternalLocationResult> ParseOsmResponse(string json, double centerLat, double centerLng)
    {
        var results = new List<ExternalLocationResult>();

        try
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;

                if (!root.TryGetProperty("elements", out JsonElement elements))
                    return results;

                foreach (var element in elements.EnumerateArray())
                {
                    // חלץ קואורדינטות
                    double? lat = null;
                    double? lng = null;

                    if (element.TryGetProperty("lat", out var latProp))
                        lat = latProp.GetDouble();
                    if (element.TryGetProperty("lon", out var lngProp))
                        lng = lngProp.GetDouble();

                    // אם אין קואורדינטות ישירות, נסה center
                    if ((!lat.HasValue || !lng.HasValue) && element.TryGetProperty("center", out var center))
                    {
                        if (center.TryGetProperty("lat", out var centerLatProp))
                            lat = centerLatProp.GetDouble();
                        if (center.TryGetProperty("lon", out var centerLngProp))
                            lng = centerLngProp.GetDouble();
                    }

                    if (!lat.HasValue || !lng.HasValue)
                        continue;

                    // חלץ tags (המטא-נתונים)
                    if (!element.TryGetProperty("tags", out var tags))
                        continue;

                    // קבל שם ואמנות
                    string name = "Unknown";
                    string type = "unknown";

                    if (tags.TryGetProperty("name", out var nameProp))
                        name = nameProp.GetString() ?? "Unknown";

                    if (tags.TryGetProperty("amenity", out var amenityProp))
                        type = amenityProp.GetString() ?? "unknown";

                    // חשב מרחק מנקודת הרכז
                    double distance = CalculateDistance(centerLat, centerLng, lat.Value, lng.Value);

                    // קבל דירוג אם יש
                    double? rating = null;
                    if (tags.TryGetProperty("rating", out var ratingProp) && double.TryParse(ratingProp.GetString(), out var ratingValue))
                        rating = ratingValue;

                    results.Add(new ExternalLocationResult(
                        Name: NormalizeName(name),
                        Type: type.ToLower(),
                        DistanceInMeters: distance,
                        Latitude: lat.Value,
                        Longitude: lng.Value,
                        Rating: rating));
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing OSM response");
        }

        return results;
    }

    /// <summary>
    /// חשב מרחק בין שתי נקודות (Haversine Formula)
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // רדיוס כדור הארץ בקילומטרים

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c; // מרחק במטרים
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    /// <summary>
    /// בדוק אם קואורדינטות תקינות
    /// </summary>
    private static bool IsValidCoordinate(double lat, double lng)
    {
        return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180;
    }

    /// <summary>
    /// נרמל שם מקום (הסר זבל)
    /// </summary>
    private static string NormalizeName(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
    }
}