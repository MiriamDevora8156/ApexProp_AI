namespace ApexProp.Application.DTOs;

public class PriceHistoryDto
{
    public int Id { get; set; }
    public decimal Price { get; set; }
    public DateTime RecordedAt { get; set; }
}