namespace InventoryService.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;

    /// <summary>Optional caller-supplied correlation id (e.g. an order id) for traceability.</summary>
    public string? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
