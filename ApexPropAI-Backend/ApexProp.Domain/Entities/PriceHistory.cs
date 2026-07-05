namespace ApexProp.Domain.Entities;

public class PriceHistory
{
    public int Id { get; set; }
    public decimal Price { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
}