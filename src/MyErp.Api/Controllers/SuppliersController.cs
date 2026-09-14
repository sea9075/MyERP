using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken ct) =>
        Ok(await supplierService.GetAllAsync(includeInactive, ct));

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var created = await supplierService.CreateAsync(request, ct);
        return Ok(created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierDto>> Update(int id, [FromBody] UpdateSupplierRequest request, CancellationToken ct) =>
        Ok(await supplierService.UpdateAsync(id, request, ct));

    /// <summary>軟刪除（IsActive=false），見 ISupplierService 的說明。</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await supplierService.DeleteAsync(id, ct);
        return NoContent();
    }
}
