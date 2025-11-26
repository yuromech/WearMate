using WearMate.InventoryAPI.Data;
using WearMate.Shared.DTOs.Inventory;

namespace WearMate.InventoryAPI.Services;

public class InventoryService
{
    private readonly SupabaseClient _supabase;
    private readonly WarehouseService _warehouseService;

    public InventoryService(SupabaseClient supabase, WarehouseService warehouseService)
    {
        _supabase = supabase;
        _warehouseService = warehouseService;
    }

    public async Task<List<InventoryDto>> GetInventoryByProductAsync(Guid productVariantId)
    {
        var query = _supabase.From("inventory")
            .Eq("product_variant_id", productVariantId);
        return await _supabase.GetAsync<InventoryDto>(query.Build());
    }

    public async Task<InventoryDto?> GetInventoryByWarehouseAsync(Guid warehouseId, Guid productVariantId)
    {
        var query = _supabase.From("inventory")
            .Eq("warehouse_id", warehouseId)
            .Eq("product_variant_id", productVariantId);
        return await _supabase.GetSingleAsync<InventoryDto>(query.Build());
    }

    public async Task<List<InventoryDto>> GetLowStockAsync(int threshold = 10)
    {
        // Fetch by threshold on quantity, then also check available (quantity - reserved)
        var query = _supabase.From("inventory")
            .Lt("quantity", threshold + 1); // include equal threshold
        var rows = await _supabase.GetAsync<InventoryDto>(query.Build());
        return rows
            .Where(x => (x.Quantity - x.ReservedQuantity) < threshold)
            .ToList();
    }

    public async Task<int> GetLowStockThresholdAsync(int fallback = 10)
    {
        try
        {
            var query = _supabase.From("settings")
                .Eq("key", "LOW_STOCK_THRESHOLD")
                .Limit(1);
            var settings = await _supabase.GetAsync<SettingDto>(query.Build());
            var setting = settings.FirstOrDefault();
            if (setting != null && int.TryParse(setting.Value, out var parsed) && parsed > 0)
                return parsed;
        }
        catch
        {
        }
        return fallback;
    }

    public async Task<InventoryDto?> StockInAsync(StockInDto dto)
    {
        await EnsureWarehouseActive(dto.WarehouseId);
        var existing = await GetInventoryByWarehouseAsync(dto.WarehouseId, dto.ProductVariantId);

        if (existing != null)
        {
            var newQty = existing.Quantity + dto.Quantity;
            var updateData = new
            {
                quantity = newQty,
                updated_at = DateTime.UtcNow
            };
            await _supabase.PatchAsync<InventoryDto>("inventory", existing.Id, updateData);

            await LogStockChangeAsync(dto.WarehouseId, dto.ProductVariantId, "in",
                dto.Quantity, existing.Quantity, newQty, dto.Note, dto.CreatedBy);

            return await GetInventoryByWarehouseAsync(dto.WarehouseId, dto.ProductVariantId);
        }
        else
        {
            var data = new
            {
                warehouse_id = dto.WarehouseId,
                product_variant_id = dto.ProductVariantId,
                quantity = dto.Quantity,
                reserved_quantity = 0,
                updated_at = DateTime.UtcNow
            };
            var result = await _supabase.PostAsync<InventoryDto>("inventory", data);

            await LogStockChangeAsync(dto.WarehouseId, dto.ProductVariantId, "in",
                dto.Quantity, 0, dto.Quantity, dto.Note, dto.CreatedBy);

            return result;
        }
    }

    public async Task<InventoryDto?> StockOutAsync(StockOutDto dto)
    {
        await EnsureWarehouseActive(dto.WarehouseId);
        var existing = await GetInventoryByWarehouseAsync(dto.WarehouseId, dto.ProductVariantId);

        if (existing == null || existing.AvailableQuantity < dto.Quantity)
            throw new Exception("Insufficient stock");

        var newQty = existing.Quantity - dto.Quantity;
        var updateData = new
        {
            quantity = newQty,
            updated_at = DateTime.UtcNow
        };
        await _supabase.PatchAsync<InventoryDto>("inventory", existing.Id, updateData);

        await LogStockChangeAsync(dto.WarehouseId, dto.ProductVariantId, "out",
            dto.Quantity, existing.Quantity, newQty, dto.Note, dto.CreatedBy);

        return await GetInventoryByWarehouseAsync(dto.WarehouseId, dto.ProductVariantId);
    }

    public async Task<bool> TransferStockAsync(StockTransferDto dto)
    {
        await EnsureWarehouseActive(dto.FromWarehouseId);
        await EnsureWarehouseActive(dto.ToWarehouseId);
        var fromInventory = await GetInventoryByWarehouseAsync(dto.FromWarehouseId, dto.ProductVariantId);
        if (fromInventory == null || fromInventory.AvailableQuantity < dto.Quantity)
            throw new Exception("Insufficient stock for transfer");

        await StockOutAsync(new StockOutDto
        {
            WarehouseId = dto.FromWarehouseId,
            ProductVariantId = dto.ProductVariantId,
            Quantity = dto.Quantity,
            Note = $"Transfer to warehouse {dto.ToWarehouseId}",
            CreatedBy = dto.CreatedBy
        });

        await StockInAsync(new StockInDto
        {
            WarehouseId = dto.ToWarehouseId,
            ProductVariantId = dto.ProductVariantId,
            Quantity = dto.Quantity,
            Note = $"Transfer from warehouse {dto.FromWarehouseId}",
            CreatedBy = dto.CreatedBy
        });

        await LogStockChangeAsync(dto.FromWarehouseId, dto.ProductVariantId, "transfer",
            dto.Quantity, fromInventory.Quantity, fromInventory.Quantity - dto.Quantity,
            dto.Note, dto.CreatedBy);

        return true;
    }

    public async Task<InventoryDto?> AdjustStockAsync(StockAdjustmentDto dto)
    {
        await EnsureWarehouseActive(dto.WarehouseId);
        var existing = await GetInventoryByWarehouseAsync(dto.WarehouseId, dto.ProductVariantId);

        if (existing != null)
        {
            var updateData = new
            {
                quantity = dto.NewQuantity,
                updated_at = DateTime.UtcNow
            };
            await _supabase.PatchAsync<InventoryDto>("inventory", existing.Id, updateData);

            await LogStockChangeAsync(dto.WarehouseId, dto.ProductVariantId, "adjustment",
                dto.NewQuantity - existing.Quantity, existing.Quantity, dto.NewQuantity,
                dto.Note, dto.CreatedBy);

            return await GetInventoryByWarehouseAsync(dto.WarehouseId, dto.ProductVariantId);
        }

        return null;
    }

    public async Task<List<InventoryLogDto>> GetLogsAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        int limit = 100)
    {
        var query = _supabase.From("inventory_logs")
            .OrderBy("created_at", false)
            .Limit(limit);

        if (warehouseId.HasValue)
            query.Eq("warehouse_id", warehouseId.Value);

        if (productVariantId.HasValue)
            query.Eq("product_variant_id", productVariantId.Value);

        return await _supabase.GetAsync<InventoryLogDto>(query.Build());
    }

    private async Task LogStockChangeAsync(
        Guid warehouseId,
        Guid productVariantId,
        string type,
        int quantity,
        int beforeQty,
        int afterQty,
        string? note,
        Guid? createdBy)
    {
        var logData = new
        {
            warehouse_id = warehouseId,
            product_variant_id = productVariantId,
            type = type,
            quantity = quantity,
            before_quantity = beforeQty,
            after_quantity = afterQty,
            note = note,
            created_by = createdBy,
            created_at = DateTime.UtcNow
        };

        await _supabase.PostAsync<InventoryLogDto>("inventory_logs", logData);
    }

    private async Task EnsureWarehouseActive(Guid warehouseId)
    {
        var warehouse = await _warehouseService.GetWarehouseByIdAsync(warehouseId);
        if (warehouse == null || !warehouse.IsActive)
            throw new Exception("Warehouse is inactive or not found");
    }
}
