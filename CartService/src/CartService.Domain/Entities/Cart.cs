namespace CartService.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }

    /// <summary>Null for a guest cart. Set when a cart is created via the authenticated /me route,
    /// or when a guest cart is merged into one.</summary>
    public Guid? UserId { get; set; }

    public List<CartItem> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
