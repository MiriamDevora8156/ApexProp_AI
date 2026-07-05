namespace ApexProp.Application.DTOs;
public class BulkAnalysisResult
{
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public double SuccessRate => ProcessedCount > 0 ? (double)SuccessCount / ProcessedCount * 100 : 0;
}