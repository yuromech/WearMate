using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WearMate.Shared.DTOs.Cart;
using WearMate.Shared.DTOs.Products;
using WearMate.Web.Models.ViewModels;
using WearMate.Web.ApiClients;

namespace WearMate.Web.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "ShoppingCart";
    private readonly ProductApiClient _productApi;
    private readonly InventoryApiClient _inventoryApi;
    private readonly CartApiClient? _cartApi;
    private readonly OrderApiClient? _orderApi;
    private readonly ILogger<CartController> _logger;

    public CartController(
        ProductApiClient productApi,
        InventoryApiClient inventoryApi,
        ILogger<CartController> logger,
        CartApiClient? cartApi = null,
        OrderApiClient? orderApi = null)
    {
        _productApi = productApi;
        _inventoryApi = inventoryApi;
        _cartApi = cartApi;
        _orderApi = orderApi;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        List<CartItemViewModel> cartItems;

        if (userId != Guid.Empty && _cartApi != null)
        {
            try
            {
                var dbCart = await _cartApi.GetUserCartAsync(userId);
                cartItems = dbCart?.Items.Select(i => new CartItemViewModel
                {
                    ProductId = Guid.Empty,
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductName,
                    VariantInfo = i.VariantInfo,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    ProductImage = i.ProductImage
                }).ToList() ?? new List<CartItemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get cart from API, falling back to session");
                cartItems = GetCartFromSession();
            }
        }
        else
        {
            cartItems = GetCartFromSession();
        }

        return View(cartItems);
    }

    [HttpPost("/Cart/Add")]
    [HttpPost("/{culture}/Cart/Add")]
    [HttpPost("/Cart/Add/{productId:guid}/{variantId:guid}/{quantity:int?}")]
    [HttpPost("/{culture}/Cart/Add/{productId:guid}/{variantId:guid}/{quantity:int?}")]
    public async Task<IActionResult> Add(
        [FromBody] AddToCartRequest? body,
        [FromQuery] AddToCartRequest? query,
        [FromRoute] Guid? productId = null,
        [FromRoute] Guid? variantId = null,
        [FromRoute] int? quantity = null)
    {
        try
        {
            var dto = body ?? new AddToCartRequest();
            if (query != null)
            {
                if (dto.ProductId == Guid.Empty) dto.ProductId = query.ProductId;
                if (dto.VariantId == Guid.Empty) dto.VariantId = query.VariantId;
                if (dto.Quantity <= 0 && query.Quantity > 0) dto.Quantity = query.Quantity;
            }
            if (dto.ProductId == Guid.Empty && productId.HasValue) dto.ProductId = productId.Value;
            if (dto.VariantId == Guid.Empty && variantId.HasValue) dto.VariantId = variantId.Value;
            if (dto.Quantity <= 0 && quantity.HasValue && quantity.Value > 0) dto.Quantity = quantity.Value;

            return await ProcessAddToCart(dto.ProductId, dto.VariantId, dto.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return StatusCode(500, new { success = false, message = "Không thể thêm vào giỏ hàng." });
        }
    }

    [HttpGet("/Cart/Add")]
    [HttpGet("/{culture}/Cart/Add")]
    [HttpGet("/Cart/Add/{productId:guid}/{variantId:guid}/{quantity:int?}")]
    [HttpGet("/{culture}/Cart/Add/{productId:guid}/{variantId:guid}/{quantity:int?}")]
    public async Task<IActionResult> AddGet(
        [FromQuery][FromRoute] Guid productId,
        [FromQuery][FromRoute] Guid variantId,
        [FromQuery][FromRoute] int quantity = 1)
    {
        try
        {
            return await ProcessAddToCart(productId, variantId, quantity <= 0 ? 1 : quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart (GET)");
            return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    private async Task<IActionResult> ProcessAddToCart(Guid productId, Guid variantId, int quantity)
    {
        try
        {
            var queryPidRaw = Request.Query["productId"].ToString();
            var queryVidRaw = Request.Query["variantId"].ToString();
            var queryQtyRaw = Request.Query["quantity"].ToString();
            var routePidRaw = RouteData.Values.TryGetValue("productId", out var rp) ? rp?.ToString() : string.Empty;
            var routeVidRaw = RouteData.Values.TryGetValue("variantId", out var rv) ? rv?.ToString() : string.Empty;
            var routeQtyRaw = RouteData.Values.TryGetValue("quantity", out var rq) ? rq?.ToString() : string.Empty;

            if (productId == Guid.Empty && Guid.TryParse(queryPidRaw, out var qpProductId)) productId = qpProductId;
            if (variantId == Guid.Empty && Guid.TryParse(queryVidRaw, out var qpVariantId)) variantId = qpVariantId;
            if (quantity <= 0 && int.TryParse(queryQtyRaw, out var qpQty) && qpQty > 0) quantity = qpQty;
            if (productId == Guid.Empty && Guid.TryParse(routePidRaw, out var rpGuid)) productId = rpGuid;
            if (variantId == Guid.Empty && Guid.TryParse(routeVidRaw, out var rvGuid)) variantId = rvGuid;
            if (quantity <= 0 && int.TryParse(routeQtyRaw, out var rqInt) && rqInt > 0) quantity = rqInt;

            _logger.LogInformation("ProcessAddToCart: pid={ProductId}, vid={VariantId}, qty={Quantity}",
                productId, variantId, quantity);

            if (productId == Guid.Empty || variantId == Guid.Empty || quantity <= 0)
            {
                _logger.LogWarning($"Invalid data: ProductId={productId}, VariantId={variantId}, Quantity={quantity}");
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            ProductDto? product = null;
            try
            {
                product = await _productApi.GetProductByIdAsync(productId);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new { success = false, message = "Không thể kết nối đến Product Service." });
            }

            if (product == null || !product.IsActive)
                return BadRequest(new { success = false, message = "Sản phẩm không còn hiển thị." });

            var variants = (product.Variants != null && product.Variants.Any())
                ? product.Variants
                : await _productApi.GetVariantsAsync(product.Id) ?? new List<ProductVariantDto>();

            var variant = variants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null || !variant.IsActive || variant.ProductId != product.Id)
                return BadRequest(new { success = false, message = "Biến thể không hợp lệ." });

            int? available = null;
            try
            {
                var inventories = await _inventoryApi.GetInventoryByProductAsync(variant.Id);
                if (inventories != null && inventories.Any())
                {
                    available = inventories.Sum(i => i.AvailableQuantity);
                }
                else if (variant.StockQuantity > 0)
                {
                    available = variant.StockQuantity;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot reach Inventory API, skipping stock check.");
            }

            if (available.HasValue && available.Value <= 0)
                return BadRequest(new { success = false, message = "Sản phẩm đã hết hàng." });

            var userId = GetCurrentUserId();
            var priceSnapshot = (product.SalePrice ?? product.BasePrice) + variant.PriceAdjustment;
            var variantInfo = $"{variant.Color} {variant.Size}".Trim();

            if (userId != Guid.Empty)
            {
                var addRequest = new AddToCartRequestDto
                {
                    UserId = userId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = quantity,
                    UnitPrice = product.BasePrice,
                    FinalPrice = priceSnapshot,
                    ProductName = product.Name,
                    ImageUrl = product.ThumbnailUrl ?? product.Images.FirstOrDefault()?.ImageUrl,
                    Size = variant.Size,
                    Color = variant.Color
                };

                try
                {
                    var dbCart = await _cartApi.AddToCartAsync(addRequest);
                    if (dbCart != null)
                    {
                        HttpContext.Session.Remove(CartSessionKey);

                        return Ok(new
                        {
                            success = true,
                            message = "Đã thêm vào giỏ hàng",
                            cartCount = dbCart.TotalItems
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding to cart in database");
                }
            }

            var cartItems = GetCartFromSession();
            var existing = cartItems.FirstOrDefault(x => x.ProductVariantId == variant.Id);
            var desiredQty = quantity + (existing?.Quantity ?? 0);

            if (available.HasValue && desiredQty > available.Value)
                return BadRequest(new { success = false, message = "Sản phẩm không đủ tồn kho." });

            if (existing != null)
            {
                existing.Quantity = desiredQty;
                existing.Price = priceSnapshot;
                existing.VariantInfo = variantInfo;
                existing.ProductName = product.Name;
                existing.ProductId = product.Id;
                existing.ProductImage ??= product.ThumbnailUrl ?? product.Images.FirstOrDefault()?.ImageUrl;
            }
            else
            {
                cartItems.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    ProductVariantId = variant.Id,
                    ProductName = product.Name,
                    VariantInfo = variantInfo,
                    Price = priceSnapshot,
                    Quantity = quantity,
                    ProductImage = product.ThumbnailUrl ?? product.Images.FirstOrDefault()?.ImageUrl
                });
            }

            SaveCartToSession(cartItems);

            return Ok(new
            {
                success = true,
                message = "Đã thêm vào giỏ hàng",
                cartCount = cartItems.Sum(x => x.Quantity)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessAddToCart");
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(Guid productVariantId, int quantity)
    {
        var userId = GetCurrentUserId();

        if (userId != Guid.Empty)
        {
            try
            {
                var dbCart = await _cartApi.GetUserCartAsync(userId);
                var item = dbCart?.Items.FirstOrDefault(x => x.ProductVariantId == productVariantId);

                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        await _cartApi.RemoveItemAsync(item.Id);
                    }
                    else
                    {
                        var inventories = await _inventoryApi.GetInventoryByProductAsync(productVariantId);
                        var available = inventories?.Sum(i => i.AvailableQuantity) ?? 0;

                        if (available < quantity)
                        {
                            TempData["ErrorMessage"] = "Số lượng vượt quá tồn kho. Đã điều chỉnh về tối đa có thể.";
                            quantity = available;
                        }

                        if (quantity > 0)
                        {
                            await _cartApi.UpdateQuantityAsync(new UpdateCartItemDto
                            {
                                CartItemId = item.Id,
                                Quantity = quantity
                            });
                        }
                        else
                        {
                            await _cartApi.RemoveItemAsync(item.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart in database");
            }
        }
        else
        {
            var cartItems = GetCartFromSession();
            var item = cartItems.FirstOrDefault(x => x.ProductVariantId == productVariantId);

            if (item != null)
            {
                try
                {
                    var inventories = await _inventoryApi.GetInventoryByProductAsync(productVariantId);
                    var available = inventories?.Sum(i => i.AvailableQuantity) ?? 0;

                    if (available < quantity)
                    {
                        TempData["ErrorMessage"] = "Số lượng vượt quá tồn kho. Đã điều chỉnh về tối đa có thể.";
                        quantity = available;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot check inventory");
                }

                if (quantity <= 0)
                    cartItems.Remove(item);
                else
                    item.Quantity = quantity;
            }

            SaveCartToSession(cartItems);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveItem(Guid productVariantId)
    {
        var userId = GetCurrentUserId();

        if (userId != Guid.Empty)
        {
            try
            {
                var dbCart = await _cartApi.GetUserCartAsync(userId);
                var item = dbCart?.Items.FirstOrDefault(x => x.ProductVariantId == productVariantId);

                if (item != null)
                {
                    await _cartApi.RemoveItemAsync(item.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item from database");
            }
        }
        else
        {
            var cartItems = GetCartFromSession();
            var item = cartItems.FirstOrDefault(x => x.ProductVariantId == productVariantId);

            if (item != null)
                cartItems.Remove(item);

            SaveCartToSession(cartItems);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Clear()
    {
        var userId = GetCurrentUserId();

        if (userId != Guid.Empty)
        {
            try
            {
                await _cartApi.ClearCartAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart in database");
            }
        }

        HttpContext.Session.Remove(CartSessionKey);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> MergeCart()
    {
        var userId = GetCurrentUserId();

        if (userId == Guid.Empty)
        {
            return RedirectToAction("Login", "Auth");
        }

        var sessionCart = GetCartFromSession();

        if (sessionCart.Any())
        {
            try
            {
                var mergeRequest = new MergeCartRequestDto
                {
                    UserId = userId,
                    SessionItems = sessionCart.Select(item => new SessionCartItemDto
                    {
                        ProductId = item.ProductId,
                        VariantId = item.ProductVariantId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        FinalPrice = item.Price,
                        ProductName = item.ProductName,
                        ImageUrl = item.ProductImage,
                        Size = item.Size,
                        Color = item.Color
                    }).ToList()
                };

                await _cartApi.MergeCartAsync(mergeRequest);
                HttpContext.Session.Remove(CartSessionKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging cart");
            }
        }

        return RedirectToAction(nameof(Index));
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

public class AddToCartRequest
{
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
}
