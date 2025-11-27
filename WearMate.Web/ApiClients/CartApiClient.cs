using WearMate.Shared.DTOs.Cart;
using WearMate.Shared.DTOs.Common;

namespace WearMate.Web.ApiClients;

public class CartApiClient : BaseApiClient
{
    public CartApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<CartDto?> GetUserCartAsync(Guid userId)
    {
        var url = $"/api/cart/user/{userId}";
        var response = await GetAsync<ApiResponse<CartDto>>(url);
        return response?.Data;
    }

    public async Task<CartDto?> AddToCartAsync(AddToCartRequestDto dto)
    {
        var url = "/api/cart/add";
        var response = await PostAsync<ApiResponse<CartDto>>(url, dto);
        return response?.Data;
    }

    public async Task<CartItemDto?> UpdateQuantityAsync(UpdateCartItemDto dto)
    {
        var url = "/api/cart/update-quantity";
        var response = await PutAsync<ApiResponse<CartItemDto>>(url, dto);
        return response?.Data;
    }

    public async Task<bool> RemoveItemAsync(Guid cartItemId)
    {
        var url = $"/api/cart/remove/{cartItemId}";
        return await DeleteAsync(url);
    }

    public async Task<bool> ClearCartAsync(Guid userId)
    {
        var url = $"/api/cart/clear/{userId}";
        return await DeleteAsync(url);
    }

    public async Task<CartDto?> MergeCartAsync(MergeCartRequestDto dto)
    {
        var url = "/api/cart/merge";
        var response = await PostAsync<ApiResponse<CartDto>>(url, dto);
        return response?.Data;
    }
}
