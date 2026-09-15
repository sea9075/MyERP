using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>屬於既有 ERP 模組，只有 Product/Manager/Admin 能用（新增：權限系統，HR 部門看不到）。</summary>
[ApiController]
[Route("api/purchase-orders")]
[Authorize(Roles = "Product,Manager,Admin")]
public class PurchaseOrdersController(IPurchaseOrderService purchaseOrderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PurchaseOrderDto>>> Search(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? supplierId,
        CancellationToken ct) =>
        Ok(await purchaseOrderService.SearchAsync(dateFrom, dateTo, supplierId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(int id, CancellationToken ct) =>
        Ok(await purchaseOrderService.GetByIdAsync(id, ct));

    /// <summary>建立進貨單並自動加庫存。庫存/交易細節見 PurchaseOrderService.CreateAsync 的註解。</summary>
    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var created = await purchaseOrderService.CreateAsync(request, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    /// <summary>
    /// 作廢進貨單並把庫存扣回去（ERP.md §8 Phase 2 項目 8）。
    /// 如果扣回去會讓庫存變負的（貨已經被後續出貨單賣掉一部分），回 400。
    /// </summary>
    [HttpPost("{id:int}/void")]
    public async Task<ActionResult<PurchaseOrderDto>> Void(int id, CancellationToken ct)
    {
        var voided = await purchaseOrderService.VoidAsync(id, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(voided);
    }
}
