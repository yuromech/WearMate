namespace WearMate.Shared.DTOs.Inventory;

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateWarehouseDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}

public class InventoryDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public WarehouseDto? Warehouse { get; set; }
}

public class InventoryLogDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductVariantId { get; set; }
    public string Type { get; set; } = string.Empty; // in, out, transfer, adjustment
    public int Quantity { get; set; }
    public int BeforeQuantity { get; set; }
    public int AfterQuantity { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===== OPERATION DTOs =====

/// <summary>
/// Base class for stock operations
/// </summary>
public class StockOperationDto
{
    public Guid WarehouseId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedBy { get; set; }
}

public class StockTransferDto
{
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedBy { get; set; }
}

public class InventoryAdjustDto
{
    public Guid WarehouseId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int NewQuantity { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedBy { get; set; }
}

// Add legacy DTO class names expected by InventoryAPI controllers
public class StockInDto : StockOperationDto { }
public class StockOutDto : StockOperationDto { }
public class StockAdjustmentDto : InventoryAdjustDto { }