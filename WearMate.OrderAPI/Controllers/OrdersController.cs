using Microsoft.AspNetCore.Mvc;
using WearMate.OrderAPI.Services;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Orders;

namespace WearMate.OrderAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderDto>>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var result = await _orderService.GetOrdersAsync(page, pageSize, userId, status);
            return Ok(ApiResponse<PaginatedResult<OrderDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return StatusCode(500, ApiResponse<PaginatedResult<OrderDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(Guid id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("number/{orderNumber}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrderByNumber(string orderNumber)
    {
        try
        {
            var order = await _orderService.GetOrderByNumberAsync(orderNumber);
            if (order == null)
                return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderDto>>>> GetUserOrders(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _orderService.GetOrdersAsync(page, pageSize, userId, null);
            return Ok(ApiResponse<PaginatedResult<OrderDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user orders");
            return StatusCode(500, ApiResponse<PaginatedResult<OrderDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(dto);
            if (order == null)
                return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse("Failed to create order"));
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id },
                ApiResponse<OrderDto>.SuccessResponse(order, "Order created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrderStatus(
        Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            dto.OrderId = id;
            var order = await _orderService.UpdateOrderStatusAsync(dto);
            if (order == null)
                return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order status updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CancelOrder(
        Guid id, [FromBody] CancelOrderDto dto)
    {
        try
        {
            dto.OrderId = id;
            var order = await _orderService.CancelOrderAsync(dto);
            if (order == null)
                return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
    }
}