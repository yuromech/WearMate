using Microsoft.AspNetCore.Mvc;
using WearMate.OrderAPI.Data;
using WearMate.OrderAPI.Data.Models;
using WearMate.Shared.DTOs.Cart;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.Helpers;
using System.Text.Json;

namespace WearMate.OrderAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly SupabaseClient _db;
    private readonly ILogger<CartController> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CartController(SupabaseClient db, ILogger<CartController> logger)
    {
        _db = db;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetUserCart(Guid userId)
    {
        try
        {
            var cartUrl = _db.From("carts")
                .Select("*")
                .Eq("user_id", userId.ToString())
                .Build();

            var cart = await _db.GetSingleAsync<Cart>(cartUrl);

            if (cart == null)
            {
                return Ok(new ApiResponse<CartDto>
                {
                    Success = true,
                    Data = new CartDto { UserId = userId }
                });
            }

            var itemsUrl = _db.From("cart_items")
                .Select("*")
                .Eq("cart_id", cart.Id.ToString())
                .Build();

            var items = await _db.GetAsync<CartItem>(itemsUrl);

            var cartDto = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                Items = items.Select(i => new CartItemDto
                {
                    Id = i.Id,
                    CartId = i.CartId,
                    ProductVariantId = i.VariantId,
                    Quantity = i.Quantity,
                    Price = i.FinalPrice,
                    ProductName = i.ProductName,
                    ProductImage = i.ImageUrl,
                    VariantInfo = $"{i.Color} {i.Size}".Trim(),
                    Sku = "",
                    StockAvailable = 0
                }).ToList()
            };

            return Ok(new ApiResponse<CartDto> { Success = true, Data = cartDto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user cart");
            return StatusCode(500, new ApiResponse<CartDto>
            {
                Success = false,
                Message = "Error retrieving cart"
            });
        }
    }

    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart([FromBody] AddToCartRequestDto request)
    {
        try
        {
            if (!request.UserId.HasValue)
                return BadRequest(new ApiResponse<CartDto>
                {
                    Success = false,
                    Message = "UserId is required"
                });

            var cartUrl = _db.From("carts")
                .Select("*")
                .Eq("user_id", request.UserId.Value.ToString())
                .Build();

            var cart = await _db.GetSingleAsync<Cart>(cartUrl);

            if (cart == null)
            {
                cart = await _db.PostAsync<Cart>("carts", new
                {
                    user_id = request.UserId,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                });

                if (cart == null)
                    return StatusCode(500, new ApiResponse<CartDto>
                    {
                        Success = false,
                        Message = "Failed to create cart"
                    });
            }

            var existingItemUrl = _db.From("cart_items")
                .Select("*")
                .Eq("cart_id", cart.Id.ToString())
                .Eq("variant_id", request.VariantId.ToString())
                .Build();

            var existingItem = await _db.GetSingleAsync<CartItem>(existingItemUrl);

            if (existingItem != null)
            {
                var updatedItem = await _db.PatchAsync<CartItem>("cart_items", existingItem.Id, new
                {
                    quantity = existingItem.Quantity + request.Quantity,
                    final_price = request.FinalPrice,
                    product_name = request.ProductName,
                    image_url = request.ImageUrl,
                    size = request.Size,
                    color = request.Color,
                    updated_at = DateTime.UtcNow
                });
            }
            else
            {
                await _db.PostAsync<CartItem>("cart_items", new
                {
                    cart_id = cart.Id,
                    product_id = request.ProductId,
                    variant_id = request.VariantId,
                    quantity = request.Quantity,
                    unit_price = request.UnitPrice,
                    final_price = request.FinalPrice,
                    product_name = request.ProductName,
                    image_url = request.ImageUrl,
                    size = request.Size,
                    color = request.Color,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                });
            }

            await _db.PatchAsync<Cart>("carts", cart.Id, new
            {
                updated_at = DateTime.UtcNow
            });

            return await GetUserCart(request.UserId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return StatusCode(500, new ApiResponse<CartDto>
            {
                Success = false,
                Message = "Error adding to cart"
            });
        }
    }

    [HttpPut("update-quantity")]
    public async Task<ActionResult<ApiResponse<CartItemDto>>> UpdateQuantity([FromBody] UpdateCartItemDto request)
    {
        try
        {
            var updatedItem = await _db.PatchAsync<CartItem>("cart_items", request.CartItemId, new
            {
                quantity = request.Quantity,
                updated_at = DateTime.UtcNow
            });

            if (updatedItem == null)
                return NotFound(new ApiResponse<CartItemDto>
                {
                    Success = false,
                    Message = "Cart item not found"
                });

            var itemDto = new CartItemDto
            {
                Id = updatedItem.Id,
                CartId = updatedItem.CartId,
                ProductVariantId = updatedItem.VariantId,
                Quantity = updatedItem.Quantity,
                Price = updatedItem.FinalPrice,
                ProductName = updatedItem.ProductName,
                ProductImage = updatedItem.ImageUrl,
                VariantInfo = $"{updatedItem.Color} {updatedItem.Size}".Trim()
            };

            return Ok(new ApiResponse<CartItemDto> { Success = true, Data = itemDto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart quantity");
            return StatusCode(500, new ApiResponse<CartItemDto>
            {
                Success = false,
                Message = "Error updating quantity"
            });
        }
    }

    [HttpDelete("remove/{cartItemId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveItem(Guid cartItemId)
    {
        try
        {
            await _db.DeleteAsync("cart_items", cartItemId);

            return Ok(new ApiResponse<bool> { Success = true, Data = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Error removing item"
            });
        }
    }

    [HttpDelete("clear/{userId}")]
    public async Task<ActionResult<ApiResponse<bool>>> ClearCart(Guid userId)
    {
        try
        {
            var cartUrl = _db.From("carts")
                .Select("*")
                .Eq("user_id", userId.ToString())
                .Build();

            var cart = await _db.GetSingleAsync<Cart>(cartUrl);

            if (cart != null)
            {
                await _db.DeleteWhereAsync("cart_items", $"cart_id=eq.{cart.Id}");
            }

            return Ok(new ApiResponse<bool> { Success = true, Data = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Error clearing cart"
            });
        }
    }

    [HttpPost("merge")]
    public async Task<ActionResult<ApiResponse<CartDto>>> MergeCart([FromBody] MergeCartRequestDto request)
    {
        try
        {
            if (!request.UserId.HasValue)
                return BadRequest(new ApiResponse<CartDto>
                {
                    Success = false,
                    Message = "UserId is required"
                });

            var cartUrl = _db.From("carts")
                .Select("*")
                .Eq("user_id", request.UserId.Value.ToString())
                .Build();

            var cart = await _db.GetSingleAsync<Cart>(cartUrl);

            if (cart == null)
            {
                cart = await _db.PostAsync<Cart>("carts", new
                {
                    user_id = request.UserId,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                });

                if (cart == null)
                    return StatusCode(500, new ApiResponse<CartDto>
                    {
                        Success = false,
                        Message = "Failed to create cart"
                    });
            }

            foreach (var sessionItem in request.SessionItems)
            {
                var existingItemUrl = _db.From("cart_items")
                    .Select("*")
                    .Eq("cart_id", cart.Id.ToString())
                    .Eq("variant_id", sessionItem.VariantId.ToString())
                    .Build();

                var existingItem = await _db.GetSingleAsync<CartItem>(existingItemUrl);

                if (existingItem != null)
                {
                    await _db.PatchAsync<CartItem>("cart_items", existingItem.Id, new
                    {
                        quantity = existingItem.Quantity + sessionItem.Quantity,
                        final_price = sessionItem.FinalPrice,
                        updated_at = DateTime.UtcNow
                    });
                }
                else
                {
                    await _db.PostAsync<CartItem>("cart_items", new
                    {
                        cart_id = cart.Id,
                        product_id = sessionItem.ProductId,
                        variant_id = sessionItem.VariantId,
                        quantity = sessionItem.Quantity,
                        unit_price = sessionItem.UnitPrice,
                        final_price = sessionItem.FinalPrice,
                        product_name = sessionItem.ProductName,
                        image_url = sessionItem.ImageUrl,
                        size = sessionItem.Size,
                        color = sessionItem.Color,
                        created_at = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow
                    });
                }
            }

            await _db.PatchAsync<Cart>("carts", cart.Id, new
            {
                updated_at = DateTime.UtcNow
            });

            return await GetUserCart(request.UserId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging cart");
            return StatusCode(500, new ApiResponse<CartDto>
            {
                Success = false,
                Message = "Error merging cart"
            });
        }
    }
}
