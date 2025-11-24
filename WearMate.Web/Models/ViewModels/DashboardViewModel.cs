namespace WearMate.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        // ===== STAT CARDS =====
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBrands { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        // ===== ORDER STATISTICS =====
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TodayRevenue { get; set; }

        // ===== CUSTOMER STATISTICS =====
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }

        // ===== INVENTORY STATISTICS =====
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }

        // ===== RECENT DATA =====
        public List<OrderSummary> RecentOrders { get; set; } = new();
        public List<ProductSummary> TopSellingProducts { get; set; } = new();

        // ===== GROWTH METRICS =====
        public decimal MonthlySalesGrowth { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }

        // ===== SYSTEM INFO =====
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class OrderSummary
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

    public class ProductSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
        public int StockQuantity { get; set; }
    }
}