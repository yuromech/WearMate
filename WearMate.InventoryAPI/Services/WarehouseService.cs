using WearMate.InventoryAPI.Data;
using WearMate.Shared.DTOs.Inventory;

namespace WearMate.InventoryAPI.Services;

public class WarehouseService
{
    private readonly SupabaseClient _supabase;

    public WarehouseService(SupabaseClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<WarehouseDto>> GetAllWarehousesAsync()
    {
        var query = _supabase.From("warehouses")
            .Eq("is_active", true)
            .OrderBy("name", true);
        return await _supabase.GetAsync<WarehouseDto>(query.Build());
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id)
    {
        var query = _supabase.From("warehouses").Eq("id", id);
        return await _supabase.GetSingleAsync<WarehouseDto>(query.Build());
    }

    public async Task<WarehouseDto?> GetWarehouseByCodeAsync(string code)
    {
        var query = _supabase.From("warehouses")
            .Eq("code", code)
            .Eq("is_active", true);
        return await _supabase.GetSingleAsync<WarehouseDto>(query.Build());
    }

    public async Task<WarehouseDto?> CreateWarehouseAsync(WarehouseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required");
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new ArgumentException("Code is required");

        if (await CodeExistsAsync(dto.Code))
            throw new InvalidOperationException("Warehouse code already exists");

        var data = new
        {
            name = dto.Name,
            code = dto.Code,
            address = dto.Address,
            phone = dto.Phone,
            is_active = true,
            created_at = DateTime.UtcNow
        };
        return await _supabase.PostAsync<WarehouseDto>("warehouses", data);
    }

    public async Task<WarehouseDto?> UpdateWarehouseAsync(Guid id, WarehouseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required");
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new ArgumentException("Code is required");

        if (await CodeExistsAsync(dto.Code, id))
            throw new InvalidOperationException("Warehouse code already exists");

        var data = new
        {
            name = dto.Name,
            code = dto.Code,
            address = dto.Address,
            phone = dto.Phone,
            is_active = dto.IsActive
        };
        return await _supabase.PatchAsync<WarehouseDto>("warehouses", id, data);
    }

    public async Task<bool> DeleteWarehouseAsync(Guid id)
    {
        // Soft-disable only
        var data = new { is_active = false };
        var result = await _supabase.PatchAsync<WarehouseDto>("warehouses", id, data);
        return result != null;
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null)
    {
        var query = _supabase.From("warehouses").Eq("code", code);
        var items = await _supabase.GetAsync<WarehouseDto>(query.Build());
        if (items == null || items.Count == 0) return false;
        if (excludeId.HasValue && items.Any(w => w.Id == excludeId.Value)) return items.Count > 1;
        return true;
    }
}
