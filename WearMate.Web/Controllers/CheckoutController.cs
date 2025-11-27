using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WearMate.Web.ApiClients;
using WearMate.Web.Models.ViewModels;
using WearMate.Shared.DTOs.Orders;
using WearMate.Shared.DTOs.Auth;
using WearMate.Web.Middleware;

namespace WearMate.Web.Controllers;

[AuthorizeSession]
public class CheckoutController : Controller
{
    private readonly OrderApiClient _orderApi;
    private readonly IConfiguration _config;
    private const string CartSessionKey = "ShoppingCart";

    public CheckoutController(OrderApiClient orderApi, IConfiguration config)
    {
        _orderApi = orderApi;
        _config = config;
    }

    public IActionResult Index(Guid[]? selectedVariantIds = null)
    {
        var cartItems = GetCartFromSession();
        if (!cartItems.Any())
            return RedirectToAction("Index", "Cart");

        if (selectedVariantIds != null && selectedVariantIds.Any())
        {
            cartItems = cartItems
                .Where(x => selectedVariantIds.Contains(x.ProductVariantId))
                .ToList();
        }

        var viewModel = new CheckoutViewModel
        {
            CartItems = cartItems,
            Subtotal = cartItems.Sum(x => x.TotalPrice),
            ShippingFee = CalculateShipping(cartItems.Sum(x => x.TotalPrice)),
            SelectedVariantIds = selectedVariantIds?.ToList() ?? new List<Guid>()
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var cartItems = GetCartFromSession();
        if (model.SelectedVariantIds.Any())
        {
            cartItems = cartItems
                .Where(x => model.SelectedVariantIds.Contains(x.ProductVariantId))
                .ToList();
        }
        if (!cartItems.Any())
            return RedirectToAction("Index", "Cart");

        var userId = GetCurrentUserId();

        var orderDto = new CreateOrderDto
        {
            UserId = userId == Guid.Empty ? null : userId,
            ShippingFullName = model.ShippingFullName,
            ShippingPhone = model.ShippingPhone,
            ShippingAddress = model.ShippingAddress,
            ShippingCity = model.ShippingCity,
            PaymentMethod = model.PaymentMethod,
            Items = cartItems.Select(x => new CreateOrderItemDto
            {
                ProductVariantId = x.ProductVariantId,
                Quantity = x.Quantity
            }).ToList()
        };

        var order = await _orderApi.CreateOrderAsync(orderDto);
        if (order == null)
        {
            TempData["Error"] = "Failed to create order";
            return RedirectToAction(nameof(Index));
        }

        // Remove purchased items from session cart, keep the rest
        var remaining = GetCartFromSession()
            .Where(x => !orderDto.Items.Any(i => i.ProductVariantId == x.ProductVariantId))
            .ToList();
        SaveCartToSession(remaining);

        TempData["Success"] = "Đặt hàng thành công.";
        return RedirectToAction("Index", "Cart");
    }

    public IActionResult Success(Guid orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }

    private decimal CalculateShipping(decimal subtotal)
    {
        var defaultFee = _config.GetValue<decimal?>("Shipping:DefaultFee") ?? 30000m;
        var freeThreshold = _config.GetValue<decimal?>("Shipping:FreeShippingThreshold");
        var freeEnabled = _config.GetValue<bool?>("Shipping:FreeShippingEnabled") ?? false;

        if (freeThreshold.HasValue && subtotal >= freeThreshold.Value)
            return 0;

        if (freeEnabled && !freeThreshold.HasValue)
            return 0;

        return defaultFee;
    }

    private List<CartItemViewModel> GetCartFromSession()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
            return new List<CartItemViewModel>();

        return JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? new();
    }

    private void SaveCartToSession(List<CartItemViewModel> items)
    {
        var json = JsonSerializer.Serialize(items);
        HttpContext.Session.SetString(CartSessionKey, json);
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
