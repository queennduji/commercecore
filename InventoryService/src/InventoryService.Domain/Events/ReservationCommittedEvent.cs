namespace InventoryService.Domain.Events;

public class ReservationCommittedEvent
{
    public Guid ReservationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public DateTime CommittedAt { get; set; }
}
