using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WearMate.Web.Models.ViewModels;

namespace WearMate.Web.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "ShoppingCart";

    public IActionResult Index()
    {
        var cartItems = GetCartFromSession();
        return View(cartItems);
    }

    [HttpPost]
    public IActionResult AddToCart(Guid productVariantId, string productName, decimal price, int quantity = 1)
    {
        var cartItems = GetCartFromSession();
        var existingItem = cartItems.FirstOrDefault(x => x.ProductVariantId == productVariantId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cartItems.Add(new CartItemViewModel
            {
                ProductVariantId = productVariantId,
                ProductName = productName,
                Price = price,
                Quantity = quantity
            });
        }

        SaveCartToSession(cartItems);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult UpdateQuantity(Guid productVariantId, int quantity)
    {
        var cartItems = GetCartFromSession();
        var item = cartItems.FirstOrDefault(x => x.ProductVariantId == productVariantId);

        if (item != null)
        {
            if (quantity <= 0)
                cartItems.Remove(item);
            else
                item.Quantity = quantity;
        }

        SaveCartToSession(cartItems);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult RemoveItem(Guid productVariantId)
    {
        var cartItems = GetCartFromSession();
        var item = cartItems.FirstOrDefault(x => x.ProductVariantId == productVariantId);

        if (item != null)
            cartItems.Remove(item);

        SaveCartToSession(cartItems);
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartSessionKey);
        return RedirectToAction(nameof(Index));
    }

    private List<CartItemViewModel> GetCartFromSession()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
            return new List<CartItemViewModel>();

        return JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? new();
    }

    private void SaveCartToSession(List<CartItemViewModel> cartItems)
    {
        var json = JsonSerializer.Serialize(cartItems);
        HttpContext.Session.SetString(CartSessionKey, json);
    }
}