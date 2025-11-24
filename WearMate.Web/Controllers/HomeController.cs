using Microsoft.AspNetCore.Mvc;
using WearMate.Web.ApiClients;
using WearMate.Web.Models.ViewModels;

namespace WearMate.Web.Controllers;

public class HomeController : Controller
{
    private readonly ProductApiClient _productApi;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ProductApiClient productApi, ILogger<HomeController> logger)
    {
        _productApi = productApi;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new HomeViewModel();

        try
        {
            viewModel.FeaturedProducts = await _productApi.GetFeaturedProductsAsync(8) ?? new();
            viewModel.NewArrivals = await _productApi.GetFeaturedProductsAsync(8) ?? new();
            viewModel.BestSellers = await _productApi.GetFeaturedProductsAsync(8) ?? new();
            viewModel.FlashSaleProducts = await _productApi.GetFeaturedProductsAsync(6) ?? new();
            viewModel.Categories = await _productApi.GetCategoriesAsync() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
        }

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}