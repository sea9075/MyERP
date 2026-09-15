using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// ERP 商品模組。查詢類動作（Search/GetById/GetByBarcode）額外開放給 Support 部門唯讀存取，
/// 因為 Support 建立出貨單時需要從商品下拉選單挑商品；新增/修改/刪除商品仍只有
/// Product/Manager/Admin 能做，Support 不能管理商品本身（新增：客服部門權限）。
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>GET /api/products?keyword=&amp;categoryId=&amp;lowStock=&amp;includeDeleted=（ERP.md §6）。</summary>
    [HttpGet]
    [Authorize(Roles = "Product,Manager,Admin,Support")]
    public async Task<ActionResult<List<ProductDto>>> Search(
        [FromQuery] string? keyword,
        [FromQuery] int? categoryId,
        [FromQuery] bool? lowStock,
        [FromQuery] bool includeDeleted,
        CancellationToken ct) =>
        Ok(await productService.SearchAsync(keyword, categoryId, lowStock, includeDeleted, ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Product,Manager,Admin,Support")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct) =>
        Ok(await productService.GetByIdAsync(id, ct));

    /// <summary>GET /api/products/by-barcode/{barcode}（ERP.md §6）：條碼掃描機輸入後前端直接查詢用。</summary>
    [HttpGet("by-barcode/{barcode}")]
    [Authorize(Roles = "Product,Manager,Admin,Support")]
    public async Task<ActionResult<ProductDto>> GetByBarcode(string barcode, CancellationToken ct) =>
        Ok(await productService.GetByBarcodeAsync(barcode, ct));

    [HttpPost]
    [Authorize(Roles = "Product,Manager,Admin")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var created = await productService.CreateAsync(request, this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Product,Manager,Admin")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        Ok(await productService.UpdateAsync(id, request, this.GetCurrentUsername(), ct));

    /// <summary>軟刪除（IsDeleted=true）。</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Product,Manager,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await productService.DeleteAsync(id, this.GetCurrentUsername(), ct);
        return NoContent();
    }
}
