namespace WearMate.Shared.DTOs.Cart;

public class CartDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
    public int TotalItems => Items.Sum(i => i.Quantity);
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice => Quantity * Price;
    public DateTime CreatedAt { get; set; }

    // Product info (for display)
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string? VariantInfo { get; set; } // e.g., "Size: L, Color: Red"
    public string? Sku { get; set; }
    public int StockAvailable { get; set; }
}

// Operation DTOs
public class AddToCartDto
{
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateCartItemDto
{
    public Guid CartItemId { get; set; }
    public int Quantity { get; set; }
}

public class RemoveFromCartDto
{
    public Guid CartItemId { get; set; }
}