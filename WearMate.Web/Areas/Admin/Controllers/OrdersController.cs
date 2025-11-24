using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Orders;
using WearMate.Web.ApiClients;
using WearMate.Web.Middleware;
using WearMate.Web.Models.ViewModels;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class OrdersController : Controller
{
    private readonly OrderApiClient _orderApi;
    private readonly HttpClient _httpClient;

    public OrdersController(OrderApiClient orderApi, IHttpClientFactory httpClientFactory)
    {
        _orderApi = orderApi;
        _httpClient = httpClientFactory.CreateClient("OrderAPI");
    }

    public async Task<IActionResult> Index(int page = 1, string? status = null)
    {
        var viewModel = new OrderListViewModel
        {
            CurrentPage = page,
            FilterStatus = status
        };

        try
        {
            var url = $"/api/orders?page={page}&pageSize=20";
            if (!string.IsNullOrEmpty(status))
                url += $"&status={status}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<PaginatedResult<OrderDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var orders = apiResponse?.Data?.Items ?? new List<OrderDto>();
                
                viewModel.Orders = orders.Select(o => new OrderSummaryItem
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.ShippingFullName,
                    ItemCount = o.Items?.Count ?? 0,
                    TotalAmount = o.Total,
                    Status = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    CreatedAt = o.CreatedAt
                }).ToList();

                viewModel.TotalPages = apiResponse?.Data?.TotalPages ?? 1;
                viewModel.TotalOrders = apiResponse?.Data?.TotalCount ?? 0;
                
                // Calculate statistics
                viewModel.PendingOrders = orders.Count(o => o.Status?.ToLower() == "pending");
                viewModel.CompletedOrders = orders.Count(o => o.Status?.ToLower() == "delivered");
                viewModel.CancelledOrders = orders.Count(o => o.Status?.ToLower() == "cancelled");
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading orders: {ex.Message}";
        }

        return View(viewModel);
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        try
        {
            var order = await _orderApi.GetOrderByIdAsync(id);
            if (order == null)
            {
                TempData["Error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, string status, string? note = null)
    {
        try
        {
            var currentUser = GetCurrentUserId();

            var payload = new
            {
                NewStatus = status,
                Note = note,
                ChangedBy = currentUser
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync($"/api/orders/{id}/status", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Order status updated to '{status}' successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to update order status: {error}";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id, string reason)
    {
        try
        {
            var success = await _orderApi.CancelOrderAsync(id, reason);
            if (success)
            {
                TempData["Success"] = "Order cancelled successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to cancel order";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    private Guid GetCurrentUserId()
    {
        var sessionJson = HttpContext.Session.GetString("UserSession");
        if (string.IsNullOrEmpty(sessionJson)) return Guid.Empty;

        try
        {
            var sessionData = JsonSerializer.Deserialize<JsonElement>(sessionJson);
            var userIdStr = sessionData.GetProperty("User").GetProperty("Id").GetString();
            return Guid.Parse(userIdStr ?? Guid.Empty.ToString());
        }
        catch
        {
            return Guid.Empty;
        }
    }
}