using WearMate.OrderAPI.Data;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Orders;
using WearMate.Shared.Helpers;

namespace WearMate.OrderAPI.Services;

public class OrderService
{
    private readonly SupabaseClient _supabase;
    private readonly HttpClient _productApi;
    private readonly HttpClient _inventoryApi;

    public OrderService(SupabaseClient supabase, IHttpClientFactory httpClientFactory)
    {
        _supabase = supabase;
        _productApi = httpClientFactory.CreateClient("ProductAPI");
        _inventoryApi = httpClientFactory.CreateClient("InventoryAPI");
    }

    public async Task<PaginatedResult<OrderDto>> GetOrdersAsync(
        int page = 1,
        int pageSize = 20,
        Guid? userId = null,
        string? status = null)
    {
        page = PaginationHelper.ValidatePage(page);
        pageSize = PaginationHelper.ValidatePageSize(pageSize);
        var offset = PaginationHelper.CalculateOffset(page, pageSize);

        var query = _supabase.From("orders");

        if (userId.HasValue)
            query.Eq("user_id", userId.Value);

        if (!string.IsNullOrEmpty(status))
            query.Eq("status", status);

        query.OrderBy("created_at", false).Limit(pageSize).Offset(offset);

        var orders = await _supabase.GetAsync<OrderDto>(query.Build());

        foreach (var order in orders)
        {
            var itemsQuery = _supabase.From("order_items")
                .Eq("order_id", order.Id);
            order.Items = await _supabase.GetAsync<OrderItemDto>(itemsQuery.Build());
        }

        var countQuery = _supabase.From("orders");
        if (userId.HasValue) countQuery.Eq("user_id", userId.Value);
        if (!string.IsNullOrEmpty(status)) countQuery.Eq("status", status);
        var totalCount = await _supabase.GetCountAsync(countQuery.Build());

        return PaginationHelper.CreateResult(orders, totalCount, page, pageSize);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
    {
        var query = _supabase.From("orders").Eq("id", id);
        var order = await _supabase.GetSingleAsync<OrderDto>(query.Build());

        if (order != null)
        {
            var itemsQuery = _supabase.From("order_items").Eq("order_id", id);
            order.Items = await _supabase.GetAsync<OrderItemDto>(itemsQuery.Build());
        }

        return order;
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        var query = _supabase.From("orders").Eq("order_number", orderNumber);
        var order = await _supabase.GetSingleAsync<OrderDto>(query.Build());

        if (order != null)
        {
            var itemsQuery = _supabase.From("order_items").Eq("order_id", order.Id);
            order.Items = await _supabase.GetAsync<OrderItemDto>(itemsQuery.Build());
        }

        return order;
    }

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderDto dto)
    {
        var orderNumber = GenerateOrderNumber();
        decimal subtotal = 0;

        foreach (var item in dto.Items)
        {
            var variantResponse = await _productApi.GetAsync(
                $"/api/products/variant/{item.ProductVariantId}");

            if (!variantResponse.IsSuccessStatusCode)
                throw new Exception("Product variant not found");

            var variantJson = await variantResponse.Content.ReadAsStringAsync();
            var variant = System.Text.Json.JsonSerializer.Deserialize<dynamic>(variantJson);

            subtotal += item.Quantity * (decimal)variant.price;
        }

        var shippingFee = subtotal >= 500000 ? 0 : 30000;
        var total = subtotal + shippingFee - dto.Items.Sum(x => 0m);

        var orderData = new
        {
            order_number = orderNumber,
            user_id = dto.UserId,
            shipping_full_name = dto.ShippingFullName,
            shipping_phone = dto.ShippingPhone,
            shipping_address = dto.ShippingAddress,
            shipping_ward = dto.ShippingWard,
            shipping_district = dto.ShippingDistrict,
            shipping_city = dto.ShippingCity,
            subtotal = subtotal,
            shipping_fee = shippingFee,
            discount_amount = 0m,
            total = total,
            status = "pending",
            payment_method = dto.PaymentMethod,
            payment_status = "unpaid",
            note = dto.Note,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        var order = await _supabase.PostAsync<OrderDto>("orders", orderData);

        if (order != null)
        {
            foreach (var item in dto.Items)
            {
                var itemData = new
                {
                    order_id = order.Id,
                    product_variant_id = item.ProductVariantId,
                    product_name = "Product",
                    quantity = item.Quantity,
                    unit_price = 0m,
                    total_price = 0m,
                    created_at = DateTime.UtcNow
                };
                await _supabase.PostAsync<OrderItemDto>("order_items", itemData);
            }

            await AddStatusHistoryAsync(order.Id, null, "pending", "Order created", dto.UserId);
        }

        return await GetOrderByIdAsync(order!.Id);
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(UpdateOrderStatusDto dto)
    {
        var order = await GetOrderByIdAsync(dto.OrderId);
        if (order == null) return null;

        var data = new
        {
            status = dto.NewStatus,
            updated_at = DateTime.UtcNow
        };

        await _supabase.PatchAsync<OrderDto>("orders", dto.OrderId, data);
        await AddStatusHistoryAsync(dto.OrderId, order.Status, dto.NewStatus, dto.Note, dto.ChangedBy);

        return await GetOrderByIdAsync(dto.OrderId);
    }

    public async Task<OrderDto?> CancelOrderAsync(CancelOrderDto dto)
    {
        var order = await GetOrderByIdAsync(dto.OrderId);
        if (order == null) return null;

        if (order.Status == "delivered" || order.Status == "cancelled")
            throw new Exception("Cannot cancel this order");

        var data = new
        {
            status = "cancelled",
            cancelled_reason = dto.Reason,
            updated_at = DateTime.UtcNow
        };

        await _supabase.PatchAsync<OrderDto>("orders", dto.OrderId, data);
        await AddStatusHistoryAsync(dto.OrderId, order.Status, "cancelled", dto.Reason, dto.CancelledBy);

        return await GetOrderByIdAsync(dto.OrderId);
    }

    private async Task AddStatusHistoryAsync(
        Guid orderId,
        string? oldStatus,
        string newStatus,
        string? note,
        Guid? changedBy)
    {
        var data = new
        {
            order_id = orderId,
            old_status = oldStatus,
            new_status = newStatus,
            note = note,
            changed_by = changedBy,
            created_at = DateTime.UtcNow
        };

        await _supabase.PostAsync<OrderStatusHistoryDto>("order_status_history", data);
    }

    private string GenerateOrderNumber()
    {
        return $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
}