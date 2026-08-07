namespace InventoryService.Domain.Events;

public class StockReservedEvent
{
    public Guid ReservationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime ReservedAt { get; set; }
}
