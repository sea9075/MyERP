using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>屬於既有 ERP 模組，只有 Product/Manager/Admin 能用（新增：權限系統，HR 部門看不到）。</summary>
[ApiController]
[Route("api/sales-orders")]
[Authorize(Roles = "Product,Manager,Admin")]
public class SalesOrdersController(ISalesOrderService salesOrderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SalesOrderDto>>> Search(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? customerId,
        CancellationToken ct) =>
        Ok(await salesOrderService.SearchAsync(dateFrom, dateTo, customerId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalesOrderDto>> GetById(int id, CancellationToken ct) =>
        Ok(await salesOrderService.GetByIdAsync(id, ct));

    /// <summary>
    /// 建立出貨單並自動扣庫存。庫存不足時整張單失敗、回 400（BusinessRuleException），
    /// 細節見 SalesOrderService.CreateAsync 的註解。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SalesOrderDto>> Create([FromBody] CreateSalesOrderRequest request, CancellationToken ct)
    {
        var created = await salesOrderService.CreateAsync(request, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    /// <summary>作廢出貨單並把庫存加回去（ERP.md §8 Phase 2 項目 8）。加回庫存不會有負數疑慮。</summary>
    [HttpPost("{id:int}/void")]
    public async Task<ActionResult<SalesOrderDto>> Void(int id, CancellationToken ct)
    {
        var voided = await salesOrderService.VoidAsync(id, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(voided);
    }
}
