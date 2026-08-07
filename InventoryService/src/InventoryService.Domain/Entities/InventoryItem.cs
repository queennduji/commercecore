namespace InventoryService.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Stock free to be reserved: on-hand minus whatever is already held for other reservations.</summary>
    public int Available => OnHand - Reserved;
}
