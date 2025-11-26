using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WearMate.Shared.DTOs.Products;
using WearMate.Web.ApiClients;
using WearMate.Web.Middleware;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class BrandsController : Controller
{
    private readonly ProductApiClient _productApi;

    public BrandsController(ProductApiClient productApi)
    {
        _productApi = productApi;
    }

    public async Task<IActionResult> Index(string? search = null, bool includeInactive = false)
    {
        var brands = await _productApi.GetBrandsAsync(includeInactive, search) ?? new();
        ViewBag.Search = search;
        ViewBag.IncludeInactive = includeInactive;
        return View(brands);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.IsEdit = false;
        return View("Form", new CreateBrandDto { IsActive = true });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBrandDto model, IFormFile? logoFile)
    {
        ViewBag.IsEdit = false;
        ModelState.Clear();
        TryValidateModel(model);

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Name is required");

        if (!ModelState.IsValid)
            return View("Form", model);

        model.Slug = BuildSlug(model.Slug, model.Name);

        if (logoFile != null && logoFile.Length > 0)
        {
            var upload = await _productApi.UploadBrandLogoAsync(logoFile);
            if (upload?.Success == true && !string.IsNullOrWhiteSpace(upload.Data))
            {
                model.LogoUrl = upload.Data;
            }
            else
            {
                AddApiErrors(upload?.Message, upload?.Errors);
                ModelState.AddModelError(nameof(model.LogoUrl), upload?.Message ?? "Failed to upload logo");
                return View("Form", model);
            }
        }

        var api = await _productApi.CreateBrandAsync(model);
        if (api?.Success == true && api.Data != null)
        {
            TempData["Success"] = "Brand created successfully!";
            return RedirectToAction(nameof(Index));
        }

        AddApiErrors(api?.Message, api?.Errors);
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var brand = await _productApi.GetBrandAsync(id);
        if (brand == null)
            return NotFound();

        var model = new CreateBrandDto
        {
            Name = brand.Name,
            Slug = brand.Slug,
            Description = brand.Description,
            LogoUrl = brand.LogoUrl,
            IsActive = brand.IsActive
        };

        ViewBag.IsEdit = true;
        ViewBag.BrandId = id;
        ViewBag.CurrentLogoUrl = brand.LogoUrl;
        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CreateBrandDto model, IFormFile? logoFile)
    {
        ViewBag.IsEdit = true;
        ViewBag.BrandId = id;
        ViewBag.CurrentLogoUrl = model.LogoUrl;

        ModelState.Clear();
        TryValidateModel(model);

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Name is required");

        if (!ModelState.IsValid)
            return View("Form", model);

        model.Slug = BuildSlug(model.Slug, model.Name);

        if (logoFile != null && logoFile.Length > 0)
        {
            var upload = await _productApi.UploadBrandLogoAsync(logoFile, model.LogoUrl);
            if (upload?.Success == true && !string.IsNullOrWhiteSpace(upload.Data))
            {
                model.LogoUrl = upload.Data;
            }
            else
            {
                AddApiErrors(upload?.Message, upload?.Errors);
                ModelState.AddModelError(nameof(model.LogoUrl), upload?.Message ?? "Failed to upload logo");
                return View("Form", model);
            }
        }

        var api = await _productApi.UpdateBrandAsync(id, model);
        if (api?.Success == true && api.Data != null)
        {
            TempData["Success"] = "Brand updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        AddApiErrors(api?.Message, api?.Errors);
        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var api = await _productApi.DeleteBrandAsync(id);
        if (api?.Success == true)
            TempData["Success"] = "Brand deleted successfully!";
        else
            TempData["Error"] = api?.Message ?? "Failed to delete brand";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var api = await _productApi.DeactivateBrandAsync(id);
        if (api?.Success == true)
            TempData["Success"] = "Brand deactivated successfully!";
        else
            TempData["Error"] = api?.Message ?? "Failed to deactivate brand";

        return RedirectToAction(nameof(Index), new { includeInactive = true });
    }

    [HttpPost]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var api = await _productApi.ReactivateBrandAsync(id);
        if (api?.Success == true)
            TempData["Success"] = "Brand reactivated successfully!";
        else
            TempData["Error"] = api?.Message ?? "Failed to reactivate brand";

        return RedirectToAction(nameof(Index), new { includeInactive = true });
    }

    private string BuildSlug(string? slug, string name)
    {
        var source = string.IsNullOrWhiteSpace(slug) ? name : slug;
        return WearMate.Shared.Helpers.SlugHelper.Slugify(source, "brand");
    }

    private void AddApiErrors(string? message, List<string>? errors)
    {
        if (!string.IsNullOrWhiteSpace(message))
            ModelState.AddModelError(string.Empty, message);

        if (errors != null)
        {
            foreach (var err in errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
        }
    }
}
