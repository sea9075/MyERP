using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// ERP 出貨單模組。Product/Manager/Admin/Support 部門都能查詢；新增／作廢（使用者決定）
/// 只開放給 Support（客服部門）——這是 Support 的核心工作範圍，商品部/主管/管理員維持唯讀。
/// 用兩層 [Authorize] 疊加：class 層級先過濾出四個部門都能查詢，Create/Void 這兩個動作
/// 額外再疊一層 Roles = "Support"，兩層是 AND 的關係，等於只有 Support 能通過。
/// </summary>
[ApiController]
[Route("api/sales-orders")]
[Authorize(Roles = "Product,Manager,Admin,Support")]
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

    /// <summary>建立出貨單並自動扣庫存。庫存細節見 SalesOrderService.CreateAsync 的註解。只有 Support 能用。</summary>
    [HttpPost]
    [Authorize(Roles = "Support")]
    public async Task<ActionResult<SalesOrderDto>> Create([FromBody] CreateSalesOrderRequest request, CancellationToken ct)
    {
        var created = await salesOrderService.CreateAsync(request, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    /// <summary>作廢出貨單並把庫存加回去（ERP.md §8 Phase 2 項目 8）。只有 Support 能用。</summary>
    [HttpPost("{id:int}/void")]
    [Authorize(Roles = "Support")]
    public async Task<ActionResult<SalesOrderDto>> Void(int id, CancellationToken ct)
    {
        var voided = await salesOrderService.VoidAsync(id, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(voided);
    }
}
