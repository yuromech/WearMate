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

    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string? VariantInfo { get; set; }
    public string? Sku { get; set; }
    public int StockAvailable { get; set; }
}

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

public class AddToCartRequestDto
{
    public Guid? UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
}

public class MergeCartRequestDto
{
    public Guid? UserId { get; set; }
    public List<SessionCartItemDto> SessionItems { get; set; } = new();
}

public class SessionCartItemDto
{
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
}