
namespace ApexProp.Application.DTOs;

public class AIAnalysisResult
{
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public double AIScore { get; set; }
    public string ScoreInterpretation { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
}