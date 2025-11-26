using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Inventory;

namespace WearMate.Web.ApiClients;

public class InventoryApiClient : BaseApiClient
{
    public InventoryApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    // ===== INVENTORY ENDPOINTS =====

    /// <summary>
    /// Get inventory for a specific product variant in a warehouse
    /// </summary>
    public async Task<List<InventoryDto>?> GetInventoryByProductAsync(Guid productVariantId)
    {
        var url = $"/api/inventory/product/{productVariantId}";
        var api = await GetAsync<ApiResponse<List<InventoryDto>>>(url);
        return api?.Data;
    }

    /// <summary>
    /// Get inventory for a specific product variant in a warehouse
    /// </summary>
    public async Task<InventoryDto?> GetInventoryByWarehouseAsync(Guid warehouseId, Guid productVariantId)
    {
        var url = $"/api/inventory/warehouse/{warehouseId}/product/{productVariantId}";
        var api = await GetAsync<ApiResponse<InventoryDto>>(url);
        return api?.Data;
    }

    /// <summary>
    /// Get low stock products
    /// </summary>
    public async Task<List<InventoryDto>?> GetLowStockAsync(int threshold = 10)
    {
        var url = $"/api/inventory/low-stock?threshold={threshold}";
        var api = await GetAsync<ApiResponse<List<InventoryDto>>>(url);
        return api?.Data;
    }

    /// <summary>
    /// Get inventory logs
    /// </summary>
    public async Task<List<InventoryLogDto>?> GetLogsAsync(Guid? warehouseId = null, Guid? productVariantId = null, int limit = 100)
    {
        var url = $"/api/inventory/logs?limit={limit}";
        if (warehouseId.HasValue)
            url += $"&warehouseId={warehouseId.Value}";
        if (productVariantId.HasValue)
            url += $"&productVariantId={productVariantId.Value}";
        var api = await GetAsync<ApiResponse<List<InventoryLogDto>>>(url);
        return api?.Data;
    }

    /// <summary>
    /// Stock in operation
    /// </summary>
    public async Task<bool> StockInAsync(StockOperationDto dto)
    {
        var url = "/api/inventory/stock-in";
        return await PostBoolAsync(url, dto);
    }

    /// <summary>
    /// Stock out operation
    /// </summary>
    public async Task<bool> StockOutAsync(StockOperationDto dto)
    {
        var url = "/api/inventory/stock-out";
        return await PostBoolAsync(url, dto);
    }

    /// <summary>
    /// Transfer stock between warehouses
    /// </summary>
    public async Task<bool> TransferStockAsync(StockTransferDto dto)
    {
        var url = "/api/inventory/transfer";
        return await PostBoolAsync(url, dto);
    }

    /// <summary>
    /// Adjust inventory quantity
    /// </summary>
    public async Task<bool> AdjustInventoryAsync(InventoryAdjustDto dto)
    {
        var url = "/api/inventory/adjust";
        return await PostBoolAsync(url, dto);
    }

    // ===== WAREHOUSE ENDPOINTS =====

    /// <summary>
    /// Get all warehouses
    /// </summary>
    public async Task<List<WarehouseDto>?> GetWarehousesAsync()
    {
        var url = "/api/warehouses";
        var api = await GetAsync<ApiResponse<List<WarehouseDto>>>(url);
        return api?.Data;
    }

    /// <summary>
    /// Get warehouse by ID
    /// </summary>
    public async Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id)
    {
        var url = $"/api/warehouses/{id}";
        var api = await GetAsync<ApiResponse<WarehouseDto>>(url);
        return api?.Data;
    }

    /// <summary>
    /// Create new warehouse
    /// </summary>
    public async Task<WarehouseDto?> CreateWarehouseAsync(CreateWarehouseDto dto)
    {
        var url = "/api/warehouses";
        return await PostAsync<WarehouseDto>(url, dto);
    }

    /// <summary>
    /// Update warehouse
    /// </summary>
    public async Task<WarehouseDto?> UpdateWarehouseAsync(Guid id, CreateWarehouseDto dto)
    {
        var url = $"/api/warehouses/{id}";
        var response = await _http.PutAsJsonAsync(url, dto);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        var api = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<WarehouseDto>>(json, _json);
        return api?.Data;
    }

    /// <summary>
    /// Delete warehouse
    /// </summary>
    public async Task<bool> DeleteWarehouseAsync(Guid id)
    {
        var url = $"/api/warehouses/{id}";
        return await DeleteAsync(url);
    }
}
