namespace WearMate.Shared.DTOs.Orders;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid? UserId { get; set; }

    // Shipping info
    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? ShippingWard { get; set; }
    public string? ShippingDistrict { get; set; }
    public string ShippingCity { get; set; } = string.Empty;

    // Pricing
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    // Status
    public string Status { get; set; } = "pending";
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "unpaid";

    public string? Note { get; set; }
    public string? CancelledReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Items
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ProductVariantId { get; set; }

    // Snapshot data
    public string ProductName { get; set; } = string.Empty;
    public string? VariantInfo { get; set; }
    public string? Sku { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class OrderStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create/Update DTOs
public class CreateOrderDto
{
    public Guid? UserId { get; set; }

    // Shipping info
    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? ShippingWard { get; set; }
    public string? ShippingDistrict { get; set; }
    public string ShippingCity { get; set; } = string.Empty;

    // Payment
    public string PaymentMethod { get; set; } = "cod";
    public string? Note { get; set; }

    // Items
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusDto
{
    public Guid OrderId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? ChangedBy { get; set; }
}

public class CancelOrderDto
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? CancelledBy { get; set; }
}