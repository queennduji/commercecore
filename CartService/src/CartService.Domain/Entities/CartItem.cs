namespace CartService.Domain.Entities;

public class CartItem
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Snapshotted from CatalogService when the item was added – stays fixed while the
    /// item sits in the cart, so a later price change in the catalog doesn't silently change
    /// what the shopper already sees in their cart.</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; }
}
