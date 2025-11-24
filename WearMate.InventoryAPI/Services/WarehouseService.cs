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
        var data = new { is_active = false };
        var result = await _supabase.PatchAsync<WarehouseDto>("warehouses", id, data);
        return result != null;
    }
}