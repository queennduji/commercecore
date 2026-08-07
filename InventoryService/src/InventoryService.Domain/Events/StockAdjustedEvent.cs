namespace InventoryService.Domain.Events;

public class StockAdjustedEvent
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int Delta { get; set; }
    public int OnHandAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; }
}
