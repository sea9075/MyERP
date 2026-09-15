using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// ERP 出貨單模組。Product/Manager/Admin/Support 部門都能查詢；新增／作廢原本只開放給
/// Support（客服部門），2026-09-15 使用者要求追加開放給 Manager/Admin（系統裡 Manager/Admin
/// 權限一直保持完全相同，這次比照辦理），Product 部門維持唯讀。
/// 用兩層 [Authorize] 疊加：class 層級先過濾出四個部門都能查詢，Create/Void 這兩個動作
/// 額外再疊一層 Roles = "Support,Manager,Admin"，兩層是 AND 的關係，等於 Product 不能通過。
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

    /// <summary>
    /// 建立出貨單並自動扣庫存。庫存細節見 SalesOrderService.CreateAsync 的註解。
    /// Support/Manager/Admin 能用（2026-09-15 追加 Manager/Admin，見本檔案類別註解）。
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Support,Manager,Admin")]
    public async Task<ActionResult<SalesOrderDto>> Create([FromBody] CreateSalesOrderRequest request, CancellationToken ct)
    {
        var created = await salesOrderService.CreateAsync(request, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    /// <summary>
    /// 作廢出貨單並把庫存加回去（ERP.md §8 Phase 2 項目 8）。
    /// Support/Manager/Admin 能用（2026-09-15 追加 Manager/Admin，見本檔案類別註解）。
    /// </summary>
    [HttpPost("{id:int}/void")]
    [Authorize(Roles = "Support,Manager,Admin")]
    public async Task<ActionResult<SalesOrderDto>> Void(int id, CancellationToken ct)
    {
        var voided = await salesOrderService.VoidAsync(id, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(voided);
    }
}
