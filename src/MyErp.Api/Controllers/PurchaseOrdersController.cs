using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
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
        var currentUserId = GetCurrentUserId();
        var created = await purchaseOrderService.CreateAsync(request, currentUserId, ct);
        return Ok(created);
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("JWT 缺少使用者 Id (NameIdentifier claim)，這裡不應該發生。");
        return int.Parse(idClaim);
    }
}
