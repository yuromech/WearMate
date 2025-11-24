using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WearMate.Shared.DTOs.Inventory;
using WearMate.Web.ApiClients;
using WearMate.Web.Middleware;
using WearMate.Web.Models.ViewModels;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class InventoryController : Controller
{
    private readonly InventoryApiClient _inventoryApi;
    private readonly ProductApiClient _productApi;

    public InventoryController(InventoryApiClient inventoryApi, ProductApiClient productApi)
    {
        _inventoryApi = inventoryApi;
        _productApi = productApi;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new InventoryIndexViewModel();

        try
        {
            viewModel.LowStock = await _inventoryApi.GetLowStockAsync(10) ?? new();
            viewModel.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading inventory: {ex.Message}";
        }

        return View(viewModel);
    }

    public async Task<IActionResult> Logs(Guid? warehouseId = null, int page = 1)
    {
        var viewModel = new InventoryLogsViewModel
        {
            SelectedWarehouseId = warehouseId,
            CurrentPage = page
        };

        try
        {
            viewModel.Logs = await _inventoryApi.GetLogsAsync(warehouseId, null, 50) ?? new();
            viewModel.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading logs: {ex.Message}";
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> StockIn()
    {
        ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();

        return View(new StockInDto());
    }

    [HttpPost]
    public async Task<IActionResult> StockIn(StockInDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
            ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();
            return View(model);
        }

        try
        {
            var currentUser = GetCurrentUserId();
            model.CreatedBy = currentUser;

            var success = await _inventoryApi.StockInAsync(model);
            if (success)
            {
                TempData["Success"] = "Stock in successful!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to stock in";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> StockOut()
    {
        ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();

        return View(new StockOutDto());
    }

    [HttpPost]
    public async Task<IActionResult> StockOut(StockOutDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
            ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();
            return View(model);
        }

        try
        {
            var currentUser = GetCurrentUserId();
            model.CreatedBy = currentUser;

            var success = await _inventoryApi.StockOutAsync(model);
            if (success)
            {
                TempData["Success"] = "Stock out successful!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to stock out. Check if quantity is available.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Transfer()
    {
        ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();

        return View(new StockTransferDto());
    }

    [HttpPost]
    public async Task<IActionResult> Transfer(StockTransferDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
            ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();
            return View(model);
        }

        try
        {
            var currentUser = GetCurrentUserId();
            model.CreatedBy = currentUser;

            var success = await _inventoryApi.TransferStockAsync(model);
            if (success)
            {
                TempData["Success"] = "Stock transfer successful!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to transfer stock";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        ViewBag.Products = await _productApi.GetProductsAsync(1, 100) ?? new();
        return View(model);
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
}