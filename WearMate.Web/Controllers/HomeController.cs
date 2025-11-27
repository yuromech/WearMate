using Microsoft.AspNetCore.Mvc;
using WearMate.Web.ApiClients;
using WearMate.Web.Models.ViewModels;
using WearMate.Shared.Helpers;

namespace WearMate.Web.Controllers;

public class HomeController : Controller
{
    private readonly ProductApiClient _productApi;
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _config;

    public HomeController(ProductApiClient productApi, IConfiguration config, ILogger<HomeController> logger)
    {
        _productApi = productApi;
        _config = config;
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

            var supabaseUrl = _config["SUPABASE_URL"];

            var bucket = _config["SUPABASE_STORAGE_BUCKET"] ?? "wear-mate";
            viewModel.HeroBanners = new List<string>
            {
                ImageHelper.GetSupabaseUrl(supabaseUrl, bucket, "herobanner/hero1.jpg"),
                ImageHelper.GetSupabaseUrl(supabaseUrl, bucket, "herobanner/hero2.jpg"),
                ImageHelper.GetSupabaseUrl(supabaseUrl, bucket, "herobanner/hero3.jpg")
            };
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
