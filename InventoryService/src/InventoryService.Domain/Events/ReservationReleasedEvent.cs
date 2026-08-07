namespace InventoryService.Domain.Events;

public class ReservationReleasedEvent
{
    public Guid ReservationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public DateTime ReleasedAt { get; set; }
}
