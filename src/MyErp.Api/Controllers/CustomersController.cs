using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// ERP.md §8 Phase 2 項目 11：客戶管理 CRUD。
/// 商品模組改版後（使用者決定）：客戶管理是 Support（客服部門）的核心工作範圍，跟出貨單一起用，
/// Manager/Admin 也能管理；商品部（Product）不需要管理客戶，這裡拿掉 Product 的存取權限，
/// 跟前端 AppLayout.tsx／App.tsx 的角色限制保持一致。
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Manager,Admin,Support")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll([FromQuery] bool includeDeleted, CancellationToken ct) =>
        Ok(await customerService.GetAllAsync(includeDeleted, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken ct) =>
        Ok(await customerService.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var created = await customerService.CreateAsync(request, this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, [FromBody] UpdateCustomerRequest request, CancellationToken ct) =>
        Ok(await customerService.UpdateAsync(id, request, this.GetCurrentUsername(), ct));

    /// <summary>軟刪除（IsDeleted=true），還有出貨單引用時會被擋下來。</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await customerService.DeleteAsync(id, this.GetCurrentUsername(), ct);
        return NoContent();
    }
}
