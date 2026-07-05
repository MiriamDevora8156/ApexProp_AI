using Microsoft.Extensions.Logging;
using ApexProp.Domain.Entities;
using ApexProp.Domain.Interfaces;
using ApexProp.Domain.Models;

namespace ApexProp.Infrastructure.Services;

/// <summary>
/// AIScoreService - חישוב ציון כדאיות השקעה לנכס
/// זה ה"מוח" של המערכת — משקלל נתונים חיצוניים + כלכליים
/// </summary>
public class AIScoreService : IAIScoreService
{
    private readonly ILogger<AIScoreService> _logger;

    // משקלי הציוניות (מה החשוב יותר?)
    private const double TransportationWeight = 0.25;
    private const double EducationWeight = 0.25;
    private const double LeisureWeight = 0.20;
    private const double HealthcareWeight = 0.15;
    private const double ShoppingWeight = 0.15;

    public AIScoreService(ILogger<AIScoreService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// חישוב ציון AI לנכס (0-100)
    /// </summary>
    public async Task<double> CalculateScoreAsync(Property property, IEnumerable<Location> nearbyLocations)
    {
        try
        {
            _logger.LogInformation("Calculating AI score for property {PropertyId}: {Title}",
                property.Id, property.Title);

            // שלב 1: חישוב ציון כלכלי
            var economicScore = CalculateEconomicScore(property);
            _logger.LogDebug("Economic score: {Score}", economicScore);

            // שלב 2: שליפת נתונים חיצוניים
            _logger.LogDebug("Fetching external locations for property {PropertyId}", property.Id);

            // שלב 3: חישוב ציון סביבתי בהתאם לנתונים החיצוניים
            var environmentalScore = CalculateEnvironmentalScore(
    nearbyLocations.ToList(),
    new List<ExternalLocationResult>(),  // רשימה ריקה — הכל כבר ב-nearbyLocations
    property.Latitude,
    property.Longitude);

            _logger.LogDebug("Environmental score: {Score}", environmentalScore);

            // שלב 4: חישוב ציון סופי (ממוצע משוקלל)
            var finalScore = (economicScore * 0.5) + (environmentalScore * 0.5);

            // מגבלה בין 0 ל-100
            finalScore = Math.Max(0, Math.Min(100, finalScore));

            _logger.LogInformation("Final AI score for property {PropertyId}: {Score}",
                property.Id, finalScore);

            return finalScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating AI score for property {PropertyId}", property.Id);
            return 50; // ערך default בעת שגיאה
        }
    }

    /// <summary>
    /// תחזוקה של מחיר בעתיד
    /// </summary>
    public async Task<double> PredictFuturePriceAsync(int propertyId, int yearsAhead)
    {
        try
        {
            if (yearsAhead < 1 || yearsAhead > 20)
            {
                _logger.LogWarning("Invalid yearsAhead: {Years}", yearsAhead);
                return 1.0; // לא גדלה
            }

            _logger.LogInformation("Predicting future price for property {PropertyId} {Years} years ahead",
                propertyId, yearsAhead);

            // בעתיד, כאן נשליף את PriceHistory ונריץ ML model
            // לעכשיו, אנחנו מחזירים growth rate פשוט (5% בשנה)
            var estimatedGrowth = 1.0 + (0.05 * yearsAhead);

            _logger.LogDebug("Estimated growth rate: {Growth}x", estimatedGrowth);

            return await Task.FromResult(estimatedGrowth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting future price for property {PropertyId}", propertyId);
            return 1.0;
        }
    }

    /// <summary>
    /// חישוב ציון כלכלי (0-100)
    /// בוחן: מחיר למ"ר, מחיר כללי, גודל
    /// </summary>
    private double CalculateEconomicScore(Property property)
    {
        var score = 50.0; // ערך בסיסי

        // חישוב מחיר למ"ר (שימושי להשוואה)
        var pricePerSqm = (double)(property.Price / (decimal)property.AreaSqm);

        // בהנחה שממוצע בישראל הוא ~30,000 שקל למ"ר
        var averagePricePerSqm = 30000.0;

        // ככל שהמחיר למ"ר נמוך יותר, הציון גבוה יותר
        var priceRatio = pricePerSqm / averagePricePerSqm;

        if (priceRatio < 0.7)
            score += 30; // מציאה מעולה
        else if (priceRatio < 0.85)
            score += 20; // מציאה טובה
        else if (priceRatio < 1.0)
            score += 10; // קצת יקר מהממוצע
        else if (priceRatio < 1.2)
            score -= 10; // יקר
        else
            score -= 20; // יקר מאוד

        // בונוס לנכסים גדולים יותר
        if (property.AreaSqm > 150)
            score += 5;
        else if (property.AreaSqm < 80)
            score -= 5;

        // בונוס למספר חדרים סביר
        if (property.Rooms >= 3 && property.Rooms <= 4)
            score += 10;

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// חישוב ציון סביבתי (0-100)
    /// בוחן: קרבה לתחבורה, חינוך, פנאי, בריאות וקניות
    /// </summary>
    private double CalculateEnvironmentalScore(
        List<Location> nearbyLocations,
        List<ExternalLocationResult> externalLocations,
        double propertyLat,
        double propertyLng)
    {
        var allLocations = new List<(string Type, double Distance)>();

        // הוסף נתונים מ-DB
        foreach (var loc in nearbyLocations)
        {
            // חשב מרחק בפועל מנתוני ה-Location
            var distance = CalculateDistanceFromCoords(
                propertyLat, propertyLng,
                loc.Latitude, loc.Longitude);

            allLocations.Add((loc.Type, distance));
        }

        // הוסף נתונים חיצוניים
        foreach (var loc in externalLocations)
        {
            allLocations.Add((loc.Type, loc.DistanceInMeters));
        }

        var categoryScores = new Dictionary<string, double>
        {
            { "transportation", 0 },
            { "education", 0 },
            { "leisure", 0 },
            { "healthcare", 0 },
            { "shopping", 0 }
        };

        // חשב ציון לכל קטגוריה
        foreach (var location in allLocations)
        {
            var category = CategorizeLocation(location.Type);
            if (!categoryScores.ContainsKey(category))
                continue;

            var proximityScore = CalculateProximityScore(location.Distance);
            categoryScores[category] = Math.Max(categoryScores[category], proximityScore);
        }

        // חשב ציון משוקלל
        var finalScore =
            (categoryScores["transportation"] * TransportationWeight) +
            (categoryScores["education"] * EducationWeight) +
            (categoryScores["leisure"] * LeisureWeight) +
            (categoryScores["healthcare"] * HealthcareWeight) +
            (categoryScores["shopping"] * ShoppingWeight);

        return Math.Max(0, Math.Min(100, finalScore));
    }

    /// <summary>
    /// חשב ציון לפי קרבה (0-100)
    /// קרוב יותר = ציון גבוה יותר
    /// </summary>
    private static double CalculateProximityScore(double distanceInMeters)
    {
        if (distanceInMeters < 200)
            return 100; // מצוין
        else if (distanceInMeters < 500)
            return 80; // טוב
        else if (distanceInMeters < 1000)
            return 60; // סביר
        else if (distanceInMeters < 2000)
            return 40; // רחוק
        else
            return 20; // רחוק מאוד
    }

    /// <summary>
    /// קטגוריז מיקום לפי סוג
    /// </summary>
    private static string CategorizeLocation(string type)
    {
        return type.ToLower() switch
        {
            "bus_station" or "train_station" or "tram_stop" or "parking" or "taxi" => "transportation",
            "school" or "kindergarten" or "university" or "college" or "library" => "education",
            "park" or "playground" or "swimming_pool" or "sports_centre" or "cinema" or "theatre" => "leisure",
            "hospital" or "clinic" or "pharmacy" or "dentist" or "veterinary" => "healthcare",
            "supermarket" or "shop" or "market" or "mall" or "bakery" => "shopping",
            "restaurant" or "cafe" or "bar" or "fast_food" => "shopping",
            _ => "unknown"
        };
    }

    /// <summary>
    /// חשב מרחק בין שתי נקודות (Haversine)
    /// </summary>
    private static double CalculateDistanceFromCoords(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // רדיוס כדור הארץ במטרים
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c; // מרחק במטרים
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}