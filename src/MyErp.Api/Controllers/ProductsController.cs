using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>GET /api/products?keyword=&amp;categoryId=&amp;lowStock=（ERP.md §6）。</summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> Search(
        [FromQuery] string? keyword,
        [FromQuery] int? categoryId,
        [FromQuery] bool? lowStock,
        CancellationToken ct) =>
        Ok(await productService.SearchAsync(keyword, categoryId, lowStock, ct));

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
        var created = await productService.CreateAsync(request, ct);
        return Ok(created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        Ok(await productService.UpdateAsync(id, request, ct));

    /// <summary>軟刪除（IsActive=false）。</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await productService.DeleteAsync(id, ct);
        return NoContent();
    }
}
