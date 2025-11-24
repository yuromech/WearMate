using Microsoft.AspNetCore.Mvc;
using WearMate.Web.ApiClients;
using WearMate.Web.Middleware;
using WearMate.Web.Models.ViewModels;
using System.Linq;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class DashboardController : Controller
{
    private readonly ProductApiClient _productApi;
    private readonly OrderApiClient _orderApi;

    public DashboardController(ProductApiClient productApi, OrderApiClient orderApi)
    {
        _productApi = productApi;
        _orderApi = orderApi;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new DashboardViewModel();

        try
        {
            var products = await _productApi.GetProductsAsync(1, 1);
            viewModel.TotalProducts = products?.TotalCount ?? 0;

            var categories = await _productApi.GetCategoriesAsync();
            viewModel.TotalCategories = categories?.Count ?? 0;

            var brands = await _productApi.GetBrandsAsync();
            viewModel.TotalBrands = brands?.Count ?? 0;

            var orders = await _orderApi.GetOrdersAsync(1, 50);
            viewModel.TotalOrders = orders?.TotalCount ?? 0;
            viewModel.TotalRevenue = orders?.Items?.Sum(o => o.Total) ?? 0;
            viewModel.PendingOrders = orders?.Items?.Count(o => o.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)) ?? 0;
            viewModel.CompletedOrders = orders?.Items?.Count(o => o.Status.Equals("completed", StringComparison.OrdinalIgnoreCase)) ?? 0;
            viewModel.CancelledOrders = orders?.Items?.Count(o => o.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)) ?? 0;
            viewModel.TodayRevenue = orders?.Items?
                .Where(o => o.CreatedAt.Date == DateTime.UtcNow.Date)
                .Sum(o => o.Total) ?? 0;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading dashboard: {ex.Message}";
        }

        return View(viewModel);
    }
}
