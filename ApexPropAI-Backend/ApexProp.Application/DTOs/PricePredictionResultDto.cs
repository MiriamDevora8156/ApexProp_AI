namespace ApexProp.Application.DTOs;

public class PricePredictionResult
{
    public int PropertyId { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PredictedPrice { get; set; }
    public int YearsAhead { get; set; }
    public double GrowthFactor { get; set; }
    public decimal PriceDifference { get; set; }
    public decimal PercentageGrowth { get; set; }
    public DateTime PredictedAt { get; set; }
}