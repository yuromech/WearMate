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

    // === Warehouses Management ===
    [HttpGet]
    public async Task<IActionResult> Warehouses(string? search = null, bool includeInactive = true)
    {
        var warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLowerInvariant();
            warehouses = warehouses
                .Where(w => w.Name.ToLowerInvariant().Contains(kw)
                         || w.Code.ToLowerInvariant().Contains(kw)
                         || (!string.IsNullOrWhiteSpace(w.Address) && w.Address.ToLowerInvariant().Contains(kw)))
                .ToList();
        }
        if (!includeInactive)
            warehouses = warehouses.Where(w => w.IsActive).ToList();

        ViewBag.Search = search;
        ViewBag.IncludeInactive = includeInactive;
        return View(warehouses);
    }

    [HttpGet]
    public IActionResult CreateWarehouse()
    {
        return View("WarehouseForm", new CreateWarehouseDto { IsActive = true });
    }

    [HttpPost]
    public async Task<IActionResult> CreateWarehouse(CreateWarehouseDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Name is required");
        if (string.IsNullOrWhiteSpace(model.Code))
            ModelState.AddModelError(nameof(model.Code), "Code is required");

        if (!ModelState.IsValid)
            return View("WarehouseForm", model);

        var result = await _inventoryApi.CreateWarehouseAsync(model);
        if (result != null)
        {
            TempData["Success"] = "Warehouse created successfully!";
            return RedirectToAction(nameof(Warehouses));
        }

        TempData["Error"] = "Failed to create warehouse";
        return View("WarehouseForm", model);
    }

    [HttpGet]
    public async Task<IActionResult> EditWarehouse(Guid id)
    {
        var wh = await _inventoryApi.GetWarehouseByIdAsync(id);
        if (wh == null) return NotFound();

        var model = new CreateWarehouseDto
        {
            Name = wh.Name,
            Code = wh.Code,
            Address = wh.Address,
            Phone = wh.Phone,
            IsActive = wh.IsActive
        };

        ViewBag.WarehouseId = id;
        return View("WarehouseForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> EditWarehouse(Guid id, CreateWarehouseDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Name is required");
        if (string.IsNullOrWhiteSpace(model.Code))
            ModelState.AddModelError(nameof(model.Code), "Code is required");

        if (!ModelState.IsValid)
        {
            ViewBag.WarehouseId = id;
            return View("WarehouseForm", model);
        }

        var updated = await _inventoryApi.UpdateWarehouseAsync(id, model);
        if (updated != null)
        {
            TempData["Success"] = "Warehouse updated successfully!";
            return RedirectToAction(nameof(Warehouses));
        }

        TempData["Error"] = "Failed to update warehouse";
        ViewBag.WarehouseId = id;
        return View("WarehouseForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> DeactivateWarehouse(Guid id)
    {
        var ok = await _inventoryApi.DeleteWarehouseAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Warehouse deactivated" : "Failed to deactivate warehouse";
        return RedirectToAction(nameof(Warehouses));
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
        ViewBag.Products = await GetProductsWithVariantsAsync();

        return View(new StockInDto());
    }

    [HttpPost]
    public async Task<IActionResult> StockIn(StockInDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
            ViewBag.Products = await GetProductsWithVariantsAsync();
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
        ViewBag.Products = await GetProductsWithVariantsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> StockOut()
    {
        ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        ViewBag.Products = await GetProductsWithVariantsAsync();

        return View(new StockOutDto());
    }

    [HttpPost]
    public async Task<IActionResult> StockOut(StockOutDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
            ViewBag.Products = await GetProductsWithVariantsAsync();
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
        ViewBag.Products = await GetProductsWithVariantsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Transfer()
    {
        ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
        ViewBag.Products = await GetProductsWithVariantsAsync();

        return View(new StockTransferDto());
    }

    [HttpPost]
    public async Task<IActionResult> Transfer(StockTransferDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Warehouses = await _inventoryApi.GetWarehousesAsync() ?? new();
            ViewBag.Products = await GetProductsWithVariantsAsync();
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
        ViewBag.Products = await GetProductsWithVariantsAsync();
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
    private async Task<List<WearMate.Shared.DTOs.Products.ProductDto>> GetProductsWithVariantsAsync()
    {
        var all = new List<WearMate.Shared.DTOs.Products.ProductDto>();
        var pageIndex = 1;
        const int pageSize = 100;

        while (true)
        {
            var page = await _productApi.GetProductsAsync(pageIndex, pageSize);
            var items = page?.Items ?? new List<WearMate.Shared.DTOs.Products.ProductDto>();
            if (!items.Any()) break;

            foreach (var p in items)
            {
                if (p.Variants == null || !p.Variants.Any())
                {
                    p.Variants = await _productApi.GetVariantsAsync(p.Id) ?? new();
                }
            }

            all.AddRange(items);
            if (items.Count < pageSize) break;
            pageIndex++;
        }

        return all;
    }

}

