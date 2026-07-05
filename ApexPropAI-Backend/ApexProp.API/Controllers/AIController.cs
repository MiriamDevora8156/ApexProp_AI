using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ApexProp.API.Models;
using ApexProp.Application.DTOs;
using ApexProp.Domain.Interfaces;
using ApexProp.Infrastructure.Services;

namespace ApexProp.API.Controllers;

/// <summary>
/// AIController - Endpoints לעבודה עם AI Scoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class AIController : ControllerBase
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IAIScoreService _aiScoreService;
    private readonly IExternalLocationService _locationService;
    private readonly ILogger<AIController> _logger;
    private readonly IConfiguration _configuration;

    public AIController(
        IPropertyRepository propertyRepository,
        IAIScoreService aiScoreService,
        IExternalLocationService locationService, 
        ILogger<AIController> logger,
        IConfiguration configuration)

    {
        _propertyRepository = propertyRepository;
        _aiScoreService = aiScoreService;
        _locationService = locationService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// POST /api/ai/analyze/{propertyId}
    /// חשב ציון AI לנכס קיים
    /// </summary>
    [HttpPost("analyze/{propertyId}")]
    public async Task<ActionResult<ApiResponse<AIAnalysisResult>>> AnalyzeProperty(int propertyId)
    {
        try
        {
            _logger.LogInformation("Starting deep analysis for property {PropertyId}", propertyId);

            // 1. שלוף את הנכס מה-DB
            var property = await _propertyRepository.GetByIdAsync(propertyId);
            if (property == null)
                return NotFound(ApiResponse<AIAnalysisResult>.CreateError("Property not found", "NOT_FOUND"));

            // 2. פנה ל-OpenStreetMap והבא מוסדות ציבור (רדיוס 1 ק"מ)
            var externalResults = await _locationService.GetNearbyLocationsAsync(
                property.Latitude,
                property.Longitude,
                1000);

            // 3. המרת התוצאות לפורמט של ה-Database שלנו (Location Entity)
            // אנחנו מנקים את הרשימה הקיימת כדי שלא יהיו כפילויות בכל הרצה
            property.NearbyLocations = externalResults.Select(ext => new ApexProp.Domain.Entities.Location
            {
                Name = ext.Name,
                Type = ext.Type,
                Latitude = ext.Latitude,
                Longitude = ext.Longitude,
                PropertyId = propertyId // קישור לנכס
            }).ToList();

            // 4. חשב את הציון (ה-Service ישתמש בנתונים שכבר הבאנו)
            var aiScore = await _aiScoreService.CalculateScoreAsync(property, property.NearbyLocations);
            property.AIScore = aiScore;

            // 5. שמירה סופית ל-Database (שומר גם את הציון וגם את רשימת המוסדות!)
            await _propertyRepository.UpdateAsync(property);

            // 6. החזרת התשובה ל-Frontend
            var result = new AIAnalysisResult
            {
                PropertyId = property.Id,
                PropertyTitle = property.Title,
                AIScore = aiScore,
                ScoreInterpretation = InterpretScore(aiScore),
                Recommendation = GenerateRecommendation(aiScore, property),
                AnalyzedAt = DateTime.UtcNow
            };

            return Ok(ApiResponse<AIAnalysisResult>.CreateSuccess(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analysis");
            return StatusCode(500, ApiResponse<AIAnalysisResult>.CreateError("Analysis failed", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// POST /api/ai/analyze-all
    /// חשב ציוני AI לכל הנכסים (עדכון גדול)
    /// </summary>
    [HttpPost("analyze-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BulkAnalysisResult>>> AnalyzeAllProperties()
    {
        try
        {
            _logger.LogInformation("Starting bulk analysis of all properties");

            // ✅ שלוף את כל הנכסים
            var allProperties = await _propertyRepository.GetAllAsync();
            var propertyList = allProperties.ToList();

            if (!propertyList.Any())
            {
                var emptyResult = new BulkAnalysisResult { ProcessedCount = 0, SuccessCount = 0, FailedCount = 0 };
                return Ok(ApiResponse<BulkAnalysisResult>.CreateSuccess(emptyResult));
            }

            var result = new BulkAnalysisResult();

            // ✅ עבור על כל נכס
            foreach (var property in propertyList)
            {
                try
                {
                    result.ProcessedCount++;

                    // קבל locations לנכס הזה
                    var locations = property.NearbyLocations ?? new List<ApexProp.Domain.Entities.Location>();

                    // חשב ציון
                    var aiScore = await _aiScoreService.CalculateScoreAsync(property, locations);

                    // עדכן
                    property.AIScore = aiScore;
                    await _propertyRepository.UpdateAsync(property);

                    result.SuccessCount++;

                    _logger.LogDebug("Analyzed property {PropertyId} with score {Score}",
                        property.Id, aiScore);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analyzing property {PropertyId}", property.Id);
                    result.FailedCount++;
                }
            }

            _logger.LogInformation(
                "Bulk analysis complete. Processed: {Processed}, Success: {Success}, Failed: {Failed}",
                result.ProcessedCount, result.SuccessCount, result.FailedCount);

            return Ok(ApiResponse<BulkAnalysisResult>.CreateSuccess(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk analysis");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<BulkAnalysisResult>.CreateError(
                    "An error occurred during bulk analysis", "BULK_ANALYSIS_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/ai/predict-price/{propertyId}?yearsAhead=5
    /// תחזוקה של מחיר בעתיד
    /// </summary>
    [HttpGet("predict-price/{propertyId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PricePredictionResult>>> PredictFuturePrice(
        int propertyId,
        [FromQuery] int yearsAhead = 5)
    {
        try
        {
            if (propertyId <= 0)
                return BadRequest(ApiResponse<PricePredictionResult>.CreateError(
                    "Invalid property ID", "INVALID_ID"));

            if (yearsAhead < 1 || yearsAhead > 20)
                return BadRequest(ApiResponse<PricePredictionResult>.CreateError(
                    "Years ahead must be between 1 and 20", "INVALID_YEARS"));

            _logger.LogInformation("Predicting price for property {PropertyId} {Years} years ahead",
                propertyId, yearsAhead);

            // ✅ שלוף את הנכס
            var property = await _propertyRepository.GetByIdAsync(propertyId);
            if (property == null)
                return NotFound(ApiResponse<PricePredictionResult>.CreateError(
                    $"Property with ID {propertyId} not found", "NOT_FOUND"));

            // ✅ חשב תחזוקה
            var growthFactor = await _aiScoreService.PredictFuturePriceAsync(propertyId, yearsAhead);
            var predictedPrice = property.Price * (decimal)growthFactor;

            var result = new PricePredictionResult
            {
                PropertyId = property.Id,
                CurrentPrice = property.Price,
                PredictedPrice = predictedPrice,
                YearsAhead = yearsAhead,
                GrowthFactor = growthFactor,
                PriceDifference = predictedPrice - property.Price,
                PercentageGrowth = ((predictedPrice - property.Price) / property.Price * 100),
                PredictedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Price prediction complete. Current: {Current}, Predicted: {Predicted}, Growth: {Growth}%",
                property.Price, predictedPrice, result.PercentageGrowth);

            return Ok(ApiResponse<PricePredictionResult>.CreateSuccess(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting price for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<PricePredictionResult>.CreateError(
                    "An error occurred during prediction", "PREDICTION_FAILED"));
        }
    }

    [HttpPost("compare-verdict")]
    public async Task<ActionResult<ApiResponse<string>>> CompareVerdict([FromBody] CompareVerdictRequest request)
    {
        // 1. משיכת המפתח מהסביבה (Environment)
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? _configuration["GEMINI_API_KEY"] ?? "";

        // בדיקה ללוג - האם המפתח נקלט?
        _logger.LogInformation("Attempting AI Analysis. API Key exists: {Exists}", !string.IsNullOrEmpty(apiKey));

        try
        {
            if (request.PropertyIds == null || request.PropertyIds.Count < 2)
                return BadRequest(ApiResponse<string>.CreateError("At least 2 properties required", "INVALID_REQUEST"));

            // 2. שליפת הנתונים מה-DB (נשאר ללא שינוי)
            var properties = new List<ApexProp.Domain.Entities.Property>();
            foreach (var id in request.PropertyIds)
            {
                var p = await _propertyRepository.GetByIdAsync(id);
                if (p != null) properties.Add(p);
            }

            var summaries = properties.Select(p =>
                $"נכס: {p.Title} | מחיר: {p.Price:N0} | שטח: {p.AreaSqm} | חדרים: {p.Rooms} | ציון AI: {p.AIScore:F1}");

            var prompt = $@"אתה יועץ נדל""ן מקצועי. השווה בין הנכסים הבאים עבור משקיע (מטרה: {request.Goal}):
{string.Join("\n", summaries)}
תן המלצה קצרה ומנומקת של 2-3 משפטים בעברית. מי הנכס המנצח ולמה?";

            // 3. יצירת הקריאה ל-Groq (השינוי המרכזי)
            using var httpClient = new HttpClient();

            var body = new
            {
                contents = new[] {
        new { parts = new[] { new { text = prompt } } }
    }
            };

            var response = await httpClient.PostAsJsonAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite-001:generateContent?key={apiKey}", body);

            if (!response.IsSuccessStatusCode)
            {
                var scored = properties
    .Select(p => new { p.Title, p.Price, Score = (double)p.AIScore })
    .OrderByDescending(p => p.Score)
    .ToList();
                var best = scored[0];
                var second = scored[1];
                var gap = best.Score - second.Score;
                var gapText = gap > 15 ? "מנצח בפער משמעותי" : gap > 5 ? "עדיף במקצת" : "עדיף בקושי";
                var priceNote = best.Price < second.Price
                    ? " בנוסף, הוא זול יותר — יתרון כפול."
                    : $" למרות שהוא יקר יותר ב-₪{(best.Price - second.Price):N0}, הציון שלו מצדיק זאת.";
                var verdict = $"על בסיס ניתוח מקומי — {best.Title} {gapText} " +
                              $"(ציון {best.Score:F1} לעומת {second.Score:F1}).{priceNote}";
                return Ok(ApiResponse<string>.CreateSuccess(verdict));
            }

            // 4. חילוץ הטקסט מהתשובה
            var json = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var text = json?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                       ?? "לא הצלחתי לנתח.";

            return Ok(ApiResponse<string>.CreateSuccess(text));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompareVerdict crashed");
            return StatusCode(500, ApiResponse<string>.CreateError("Internal server error", "CRASH"));
        }
    }

    // ============= PRIVATE HELPERS =============

    private string InterpretScore(double score)
    {
        return score switch
        {
            >= 90 => "⭐ מעולה - חזקה מאוד",
            >= 80 => "⭐⭐ טוב מאוד",
            >= 70 => "⭐⭐⭐ טוב",
            >= 60 => "⭐⭐⭐⭐ בסדר",
            >= 50 => "⭐⭐⭐⭐⭐ בינוני",
            _ => "⚠️ חלש - שקול שוב"
        };
    }

    private string GenerateRecommendation(double score, ApexProp.Domain.Entities.Property property)
    {
        var recommendation = score switch
        {
            >= 85 => "זה מומלץ מאוד להשקעה או קנייה אישית",
            >= 70 => "נכס טוב, שקול אחרים גם",
            >= 60 => "נכס סביר, יש אופציות טובות יותר",
            >= 50 => "חשוב לשקול בעיון, אולי לא הרכש הטוב ביותר",
            _ => "לא מומלץ - יש אפשרויות רבות יותר"
        };

        if (property.Rooms < 2)
            recommendation += " | הערה: מספר חדרים נמוך";

        if (property.AreaSqm < 80)
            recommendation += " | הערה: שטח קטן מדי";

        return recommendation;
    }
}