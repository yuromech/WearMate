using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WearMate.InventoryAPI.Services;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Inventory;

namespace WearMate.InventoryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(InventoryService inventoryService, ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    [HttpGet("product/{productVariantId:guid}")]
    public async Task<ActionResult<ApiResponse<List<InventoryDto>>>> GetInventoryByProduct(Guid productVariantId)
    {
        try
        {
            var inventory = await _inventoryService.GetInventoryByProductAsync(productVariantId);
            return Ok(ApiResponse<List<InventoryDto>>.SuccessResponse(inventory));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory");
            return StatusCode(500, ApiResponse<List<InventoryDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("warehouse/{warehouseId:guid}/product/{productVariantId:guid}")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> GetInventoryByWarehouse(
        Guid warehouseId, Guid productVariantId)
    {
        try
        {
            var inventory = await _inventoryService.GetInventoryByWarehouseAsync(warehouseId, productVariantId);
            if (inventory == null)
                return NotFound(ApiResponse<InventoryDto>.ErrorResponse("Inventory not found"));
            return Ok(ApiResponse<InventoryDto>.SuccessResponse(inventory));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory");
            return StatusCode(500, ApiResponse<InventoryDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<List<InventoryDto>>>> GetLowStock([FromQuery] int threshold = 10)
    {
        try
        {
            var inventory = await _inventoryService.GetLowStockAsync(threshold);
            return Ok(ApiResponse<List<InventoryDto>>.SuccessResponse(inventory));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock");
            return StatusCode(500, ApiResponse<List<InventoryDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("stock-in")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> StockIn([FromBody] StockInDto dto)
    {
        try
        {
            var result = await _inventoryService.StockInAsync(dto);
            return Ok(ApiResponse<InventoryDto>.SuccessResponse(result, "Stock in successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stock in");
            return StatusCode(500, ApiResponse<InventoryDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("stock-out")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> StockOut([FromBody] StockOutDto dto)
    {
        try
        {
            var result = await _inventoryService.StockOutAsync(dto);
            return Ok(ApiResponse<InventoryDto>.SuccessResponse(result, "Stock out successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stock out");
            return StatusCode(500, ApiResponse<InventoryDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<bool>>> TransferStock([FromBody] StockTransferDto dto)
    {
        try
        {
            var result = await _inventoryService.TransferStockAsync(dto);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Transfer successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring stock");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("adjust")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> AdjustStock([FromBody] StockAdjustmentDto dto)
    {
        try
        {
            var result = await _inventoryService.AdjustStockAsync(dto);
            if (result == null)
                return NotFound(ApiResponse<InventoryDto>.ErrorResponse("Inventory not found"));
            return Ok(ApiResponse<InventoryDto>.SuccessResponse(result, "Adjustment successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock");
            return StatusCode(500, ApiResponse<InventoryDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("logs")]
    public async Task<ActionResult<ApiResponse<List<InventoryLogDto>>>> GetLogs(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? productVariantId = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            var logs = await _inventoryService.GetLogsAsync(warehouseId, productVariantId, limit);
            return Ok(ApiResponse<List<InventoryLogDto>>.SuccessResponse(logs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs");
            return StatusCode(500, ApiResponse<List<InventoryLogDto>>.ErrorResponse(ex.Message));
        }
    }
}