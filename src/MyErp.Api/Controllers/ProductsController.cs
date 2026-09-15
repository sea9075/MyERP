using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>屬於既有 ERP 模組，只有 Product/Manager/Admin 能用（新增：權限系統，HR 部門看不到）。</summary>
[ApiController]
[Route("api/products")]
[Authorize(Roles = "Product,Manager,Admin")]
public class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>GET /api/products?keyword=&amp;categoryId=&amp;lowStock=&amp;includeDeleted=（ERP.md §6）。</summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> Search(
        [FromQuery] string? keyword,
        [FromQuery] int? categoryId,
        [FromQuery] bool? lowStock,
        [FromQuery] bool includeDeleted,
        CancellationToken ct) =>
        Ok(await productService.SearchAsync(keyword, categoryId, lowStock, includeDeleted, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct) =>
        Ok(await productService.GetByIdAsync(id, ct));

    /// <summary>GET /api/products/by-barcode/{barcode}（ERP.md §6）：條碼掃描機輸入後前端直接查詢用。</summary>
    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> GetByBarcode(string barcode, CancellationToken ct) =>
        Ok(await productService.GetByBarcodeAsync(barcode, ct));

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var created = await productService.CreateAsync(request, this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        Ok(await productService.UpdateAsync(id, request, this.GetCurrentUsername(), ct));

    /// <summary>軟刪除（IsDeleted=true）。</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await productService.DeleteAsync(id, this.GetCurrentUsername(), ct);
        return NoContent();
    }
}
