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
    private const string CartSessionKey = "ShoppingCart";

    public CheckoutController(OrderApiClient orderApi)
    {
        _orderApi = orderApi;
    }

    public IActionResult Index()
    {
        var cartItems = GetCartFromSession();
        if (!cartItems.Any())
            return RedirectToAction("Index", "Cart");

        var viewModel = new CheckoutViewModel
        {
            CartItems = cartItems,
            Subtotal = cartItems.Sum(x => x.TotalPrice),
            ShippingFee = 30000
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var cartItems = GetCartFromSession();
        if (!cartItems.Any())
            return RedirectToAction("Index", "Cart");

        var orderDto = new CreateOrderDto
        {
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

        HttpContext.Session.Remove(CartSessionKey);
        return RedirectToAction("Success", new { orderId = order.Id });
    }

    public IActionResult Success(Guid orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }

    private List<CartItemViewModel> GetCartFromSession()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
            return new List<CartItemViewModel>();

        return JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? new();
    }
}