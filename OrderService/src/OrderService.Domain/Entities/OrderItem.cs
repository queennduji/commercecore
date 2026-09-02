namespace OrderService.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    /// <summary>The InventoryService location this line is being fulfilled from.</summary>
    public Guid LocationId { get; set; }

    /// <summary>The InventoryService StockReservation backing this line – committed on Ship,
    /// released on Cancel.</summary>
    public Guid ReservationId { get; set; }
}
