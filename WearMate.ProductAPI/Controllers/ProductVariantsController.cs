using Microsoft.AspNetCore.Mvc;
using WearMate.ProductAPI.Services;
using WearMate.Shared.DTOs.Products;
using WearMate.Shared.DTOs.Common;
using System.Collections.Generic;
using System;

namespace WearMate.ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductVariantsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductVariantsController> _logger;

    public ProductVariantsController(ProductService productService, ILogger<ProductVariantsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet("by-product/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<List<ProductVariantDto>>>> GetByProduct(Guid productId)
    {
        try
        {
            var variants = await _productService.GetVariantsByProductAsync(productId);
            return Ok(ApiResponse<List<ProductVariantDto>>.SuccessResponse(variants));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variants for product {ProductId}", productId);
            return StatusCode(500, ApiResponse<List<ProductVariantDto>>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductVariantDto>>> Create(CreateProductVariantDto dto)
    {
        try
        {
            var variant = await _productService.CreateVariantAsync(dto);
            return Ok(ApiResponse<ProductVariantDto>.SuccessResponse(variant));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating variant");
            return StatusCode(500, ApiResponse<ProductVariantDto>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductVariantDto>>> Update(Guid id, UpdateProductVariantDto dto)
    {
        try
        {
            var variant = await _productService.UpdateVariantAsync(id, dto);
            return Ok(ApiResponse<ProductVariantDto>.SuccessResponse(variant));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating variant {VariantId}", id);
            return StatusCode(500, ApiResponse<ProductVariantDto>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            var ok = await _productService.DeleteVariantAsync(id);
            return Ok(ApiResponse<bool>.SuccessResponse(ok));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting variant {VariantId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }
}
