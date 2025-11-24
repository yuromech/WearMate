using Microsoft.AspNetCore.Mvc;
using WearMate.OrderAPI.Services;
using WearMate.Shared.DTOs.Common;

namespace WearMate.OrderAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly OrderService _orderService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        PaymentService paymentService,
        OrderService orderService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost("momo")]
    public async Task<ActionResult<ApiResponse<string>>> ProcessMomo(
        [FromBody] PaymentRequest request)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(request.OrderId);
            if (order == null)
                return NotFound(ApiResponse<string>.ErrorResponse("Order not found"));

            var paymentUrl = await _paymentService.ProcessMomoPaymentAsync(request.OrderId, order.Total);
            return Ok(ApiResponse<string>.SuccessResponse(paymentUrl, "Payment URL generated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Momo payment");
            return StatusCode(500, ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("vnpay")]
    public async Task<ActionResult<ApiResponse<string>>> ProcessVnPay(
        [FromBody] PaymentRequest request)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(request.OrderId);
            if (order == null)
                return NotFound(ApiResponse<string>.ErrorResponse("Order not found"));

            var paymentUrl = await _paymentService.ProcessVnPayPaymentAsync(request.OrderId, order.Total);
            return Ok(ApiResponse<string>.SuccessResponse(paymentUrl, "Payment URL generated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay payment");
            return StatusCode(500, ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("callback/{provider}")]
    public async Task<IActionResult> PaymentCallback(string provider)
    {
        try
        {
            var parameters = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            var success = await _paymentService.HandlePaymentCallbackAsync(provider, parameters);

            if (success)
                return Ok(new { message = "Payment processed" });

            return BadRequest(new { message = "Payment failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment callback");
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

public class PaymentRequest
{
    public Guid OrderId { get; set; }
}