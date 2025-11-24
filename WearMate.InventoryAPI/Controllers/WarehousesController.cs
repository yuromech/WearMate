using Microsoft.AspNetCore.Mvc;
using WearMate.InventoryAPI.Services;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Inventory;

namespace WearMate.InventoryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly WarehouseService _warehouseService;
    private readonly ILogger<WarehousesController> _logger;

    public WarehousesController(WarehouseService warehouseService, ILogger<WarehousesController> logger)
    {
        _warehouseService = warehouseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WarehouseDto>>>> GetWarehouses()
    {
        try
        {
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            return Ok(ApiResponse<List<WarehouseDto>>.SuccessResponse(warehouses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouses");
            return StatusCode(500, ApiResponse<List<WarehouseDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> GetWarehouse(Guid id)
    {
        try
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (warehouse == null)
                return NotFound(ApiResponse<WarehouseDto>.ErrorResponse("Warehouse not found"));
            return Ok(ApiResponse<WarehouseDto>.SuccessResponse(warehouse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouse");
            return StatusCode(500, ApiResponse<WarehouseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> GetWarehouseByCode(string code)
    {
        try
        {
            var warehouse = await _warehouseService.GetWarehouseByCodeAsync(code);
            if (warehouse == null)
                return NotFound(ApiResponse<WarehouseDto>.ErrorResponse("Warehouse not found"));
            return Ok(ApiResponse<WarehouseDto>.SuccessResponse(warehouse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting warehouse");
            return StatusCode(500, ApiResponse<WarehouseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> CreateWarehouse([FromBody] WarehouseDto dto)
    {
        try
        {
            var warehouse = await _warehouseService.CreateWarehouseAsync(dto);
            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse!.Id },
                ApiResponse<WarehouseDto>.SuccessResponse(warehouse, "Warehouse created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warehouse");
            return StatusCode(500, ApiResponse<WarehouseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> UpdateWarehouse(Guid id, [FromBody] WarehouseDto dto)
    {
        try
        {
            var warehouse = await _warehouseService.UpdateWarehouseAsync(id, dto);
            if (warehouse == null)
                return NotFound(ApiResponse<WarehouseDto>.ErrorResponse("Warehouse not found"));
            return Ok(ApiResponse<WarehouseDto>.SuccessResponse(warehouse, "Warehouse updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating warehouse");
            return StatusCode(500, ApiResponse<WarehouseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteWarehouse(Guid id)
    {
        try
        {
            var result = await _warehouseService.DeleteWarehouseAsync(id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Warehouse not found"));
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Warehouse deleted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting warehouse");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}