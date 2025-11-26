using System.Text;
using Microsoft.AspNetCore.Mvc;
using WearMate.Shared.DTOs.Products;
using WearMate.Web.ApiClients;
using WearMate.Web.Middleware;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class CategoriesController : Controller
{
    private readonly ProductApiClient _productApi;

    public CategoriesController(ProductApiClient productApi)
    {
        _productApi = productApi;
    }

    public async Task<IActionResult> Index(string? search = null, bool includeInactive = false)
    {
        var categories = await _productApi.GetCategoriesAsync(includeInactive, search) ?? new();

        ViewBag.IncludeInactive = includeInactive;
        ViewBag.Search = search;
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoryViewData();
        ViewBag.IsEdit = false;

        return View("Form", new CreateCategoryDto { IsActive = true });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto model, IFormFile? imageFile)
    {
        await PopulateCategoryViewData();
        ViewBag.IsEdit = false;

        model.Slug = BuildSlug(model.Slug, model.Name);
        ModelState.Clear();
        TryValidateModel(model);

        ValidateCategoryModel(model, null);

        if (!ModelState.IsValid)
            return View("Form", model);

        if (imageFile != null && imageFile.Length > 0)
        {
            var upload = await _productApi.UploadCategoryImageAsync(imageFile);
            if (upload?.Success == true && !string.IsNullOrWhiteSpace(upload.Data))
            {
                model.ImageUrl = upload.Data;
            }
            else
            {
                AddApiErrors(upload?.Message, upload?.Errors);
                ModelState.AddModelError(nameof(model.ImageUrl), upload?.Message ?? "Failed to upload image");
                return View("Form", model);
            }
        }

        var api = await _productApi.CreateCategoryAsync(model);
        if (api?.Success == true && api.Data != null)
        {
            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Index));
        }

        AddApiErrors(api?.Message, api?.Errors);
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _productApi.GetCategoryAsync(id);
        if (category == null)
            return NotFound();

        var model = new CreateCategoryDto
        {
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ParentId = category.ParentId,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        };

        await PopulateCategoryViewData(id);
        ViewBag.IsEdit = true;
        ViewBag.CategoryId = id;
        ViewBag.CurrentImageUrl = category.ImageUrl;

        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CreateCategoryDto model, IFormFile? imageFile)
    {
        await PopulateCategoryViewData(id);
        ViewBag.IsEdit = true;
        ViewBag.CategoryId = id;
        ViewBag.CurrentImageUrl = model.ImageUrl;

        model.Slug = BuildSlug(model.Slug, model.Name);
        ModelState.Clear();
        TryValidateModel(model);

        ValidateCategoryModel(model, id);

        if (!ModelState.IsValid)
            return View("Form", model);

        if (imageFile != null && imageFile.Length > 0)
        {
            var upload = await _productApi.UploadCategoryImageAsync(imageFile, model.ImageUrl);
            if (upload?.Success == true && !string.IsNullOrWhiteSpace(upload.Data))
            {
                model.ImageUrl = upload.Data;
            }
            else
            {
                AddApiErrors(upload?.Message, upload?.Errors);
                ModelState.AddModelError(nameof(model.ImageUrl), upload?.Message ?? "Failed to upload image");
                return View("Form", model);
            }
        }

        var api = await _productApi.UpdateCategoryAsync(id, model);
        if (api?.Success == true && api.Data != null)
        {
            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        AddApiErrors(api?.Message, api?.Errors);
        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var api = await _productApi.DeleteCategoryAsync(id);
        if (api?.Success == true)
        {
            TempData["Success"] = "Category deleted permanently!";
        }
        else
        {
            TempData["Error"] = api?.Message ?? "Failed to delete category";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var api = await _productApi.DeactivateCategoryAsync(id);
        if (api?.Success == true)
        {
            TempData["Success"] = "Category deactivated successfully!";
        }
        else
        {
            TempData["Error"] = api?.Message ?? "Failed to deactivate category";
        }

        return RedirectToAction(nameof(Index), new { includeInactive = true });
    }

    [HttpPost]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var api = await _productApi.ReactivateCategoryAsync(id);
        if (api?.Success == true)
        {
            TempData["Success"] = "Category reactivated successfully!";
        }
        else
        {
            TempData["Error"] = api?.Message ?? "Failed to reactivate category";
        }

        return RedirectToAction(nameof(Index), new { includeInactive = true });
    }

    private void ValidateCategoryModel(CreateCategoryDto model, Guid? currentId)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Name is required");

        if (currentId.HasValue && model.ParentId.HasValue && currentId.Value == model.ParentId.Value)
            ModelState.AddModelError(nameof(model.ParentId), "Parent category cannot be itself");
    }

    private async Task PopulateCategoryViewData(Guid? currentId = null)
    {
        var categories = await _productApi.GetCategoriesAsync(includeInactive: true) ?? new List<CategoryDto>();

        if (currentId.HasValue)
        {
            var excluded = GetDescendants(currentId.Value, categories);
            excluded.Add(currentId.Value);
            categories = categories.Where(c => !excluded.Contains(c.Id)).ToList();
        }

        ViewBag.Categories = categories;
    }

    private HashSet<Guid> GetDescendants(Guid id, List<CategoryDto> categories)
    {
        var result = new HashSet<Guid>();

        void Traverse(Guid parentId)
        {
            var children = categories.Where(c => c.ParentId == parentId).ToList();
            foreach (var child in children)
            {
                if (result.Add(child.Id))
                {
                    Traverse(child.Id);
                }
            }
        }

        Traverse(id);
        return result;
    }

    private string BuildSlug(string? slug, string name)
    {
        var source = string.IsNullOrWhiteSpace(slug) ? name : slug;
        return Slugify(source);
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString();
        var normalized = text.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-') sb.Append(ch);
        }
        var slug = sb.ToString().ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "\\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "-+", "-");
        return slug;
    }

    private void AddApiErrors(string? message, List<string>? errors)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            ModelState.AddModelError(string.Empty, message);
        }

        if (errors != null)
        {
            foreach (var err in errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
        }
    }
}
