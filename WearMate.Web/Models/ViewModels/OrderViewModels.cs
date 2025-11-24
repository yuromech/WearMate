using System;
using System.Collections.Generic;
using WearMate.Shared.DTOs.Inventory;
using WearMate.Shared.DTOs.Orders;

namespace WearMate.Web.Models.ViewModels
{
    public class OrderListViewModel
    {
        public List<OrderSummaryItem> Orders { get; set; } = new();
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public string? FilterStatus { get; set; }
        public string? SearchQuery { get; set; }
    }

    public class OrderSummaryItem
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class OrderDetailViewModel
    {
        public OrderDto Order { get; set; } = new();
        public List<OrderItemDetail> ItemDetails { get; set; } = new();
    }

    public class OrderItemDetail
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string VariantInfo { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class InventoryIndexViewModel
    {
        public List<InventoryDto> LowStock { get; set; } = new();
        public List<WarehouseDto> Warehouses { get; set; } = new();
    }

    public class InventoryLogsViewModel
    {
        public List<InventoryLogDto> Logs { get; set; } = new();
        public List<WarehouseDto> Warehouses { get; set; } = new();
        public Guid? SelectedWarehouseId { get; set; }
        public int CurrentPage { get; set; }
    }

    public class UserListViewModel
    {
        public List<UserSummary> Users { get; set; } = new();
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }

    public class UserSummary
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
