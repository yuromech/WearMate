using System.Text;
using System.Text.Json;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Orders;

namespace WearMate.Web.ApiClients;

public class OrderApiClient : BaseApiClient
{
    public OrderApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    // ===== ORDER ENDPOINTS =====

    /// <summary>
    /// Create new order
    /// </summary>
    public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto dto)
    {
        var url = "/api/orders";
        var api = await PostAsync<ApiResponse<OrderDto>>(url, dto);
        return api?.Data;
    }

    /// <summary>
    /// Get user orders with pagination
    /// </summary>
    public async Task<PaginatedResult<OrderDto>?> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        var url = $"/api/orders/user/{userId}?page={page}&pageSize={pageSize}";
        var api = await GetAsync<ApiResponse<PaginatedResult<OrderDto>>>(url);
        return api?.Data;
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
    {
        var url = $"/api/orders/{id}";
        return await GetAsync<OrderDto>(url);
    }

    /// <summary>
    /// Get order by order number
    /// </summary>
    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        var url = $"/api/orders/number/{orderNumber}";
        return await GetAsync<OrderDto>(url);
    }

    /// <summary>
    /// Get all orders with filters (Admin)
    /// </summary>
    public async Task<PaginatedResult<OrderDto>?> GetOrdersAsync(int page = 1, int pageSize = 20, string? status = null, Guid? userId = null)
    {
        var url = $"/api/orders?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status))
            url += $"&status={status}";
        if (userId.HasValue)
            url += $"&userId={userId}";

        var api = await GetAsync<ApiResponse<PaginatedResult<OrderDto>>>(url);
        return api?.Data;
    }

    /// <summary>
    /// Update order status
    /// </summary>
    public async Task<OrderDto?> UpdateOrderStatusAsync(Guid orderId, string newStatus, string? note = null)
    {
        var url = $"/api/orders/{orderId}/status";
        var dto = new { Status = newStatus, Note = note };
        return await PatchAsync<OrderDto>(url, dto);
    }

    /// <summary>
    /// Cancel order
    /// </summary>
    public async Task<bool> CancelOrderAsync(Guid orderId, string reason)
    {
        var url = $"/api/orders/{orderId}/cancel";
        var dto = new { Reason = reason };
        var response = await PostAsync<OrderDto>(url, dto);
        return response != null;
    }

    // ===== PAYMENT ENDPOINTS =====

    /// <summary>
    /// Process Momo payment
    /// </summary>
    public async Task<PaymentResponseDto?> ProcessMomoPaymentAsync(PaymentRequestDto dto)
    {
        var url = "/api/payments/momo";
        return await PostAsync<PaymentResponseDto>(url, dto);
    }

    /// <summary>
    /// Process VNPay payment
    /// </summary>
    public async Task<PaymentResponseDto?> ProcessVNPayPaymentAsync(PaymentRequestDto dto)
    {
        var url = "/api/payments/vnpay";
        return await PostAsync<PaymentResponseDto>(url, dto);
    }

    /// <summary>
    /// Handle payment callback
    /// </summary>
    public async Task<bool> ProcessPaymentCallbackAsync(string provider, Dictionary<string, string> data)
    {
        var url = $"/api/payments/callback/{provider}";
        var response = await PostAsync<dynamic>(url, data);
        return response != null;
    }
}
