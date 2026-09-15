using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>屬於既有 ERP 模組，只有 Product/Manager/Admin 能用（新增：權限系統，HR 部門看不到）。</summary>
[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = "Product,Manager,Admin")]
public class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetAll([FromQuery] bool includeDeleted, CancellationToken ct) =>
        Ok(await supplierService.GetAllAsync(includeDeleted, ct));

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var created = await supplierService.CreateAsync(request, this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierDto>> Update(int id, [FromBody] UpdateSupplierRequest request, CancellationToken ct) =>
        Ok(await supplierService.UpdateAsync(id, request, this.GetCurrentUsername(), ct));

    /// <summary>軟刪除（IsDeleted=true），見 ISupplierService 的說明。</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await supplierService.DeleteAsync(id, this.GetCurrentUsername(), ct);
        return NoContent();
    }
}
