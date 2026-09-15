using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>屬於既有 ERP 模組，只有 Product/Manager/Admin 能用（新增：權限系統，HR 部門看不到）。</summary>
[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Product,Manager,Admin")]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    /// <summary>GET /api/inventory（ERP.md §4.5 / §6）：即時庫存列表。</summary>
    [HttpGet]
    public async Task<ActionResult<List<InventoryItemDto>>> GetInventory(CancellationToken ct) =>
        Ok(await inventoryService.GetInventoryAsync(ct));

    /// <summary>GET /api/inventory/{productId}/transactions（ERP.md §8 Phase 2 項目 9）：庫存異動明細查詢。</summary>
    [HttpGet("{productId:int}/transactions")]
    public async Task<ActionResult<List<InventoryTransactionDto>>> GetTransactions(int productId, CancellationToken ct) =>
        Ok(await inventoryService.GetTransactionsAsync(productId, ct));

    /// <summary>
    /// POST /api/inventory/adjust（ERP.md §8 Phase 2 項目 9）：手動盤點調整。
    /// Body 帶「調整量」（正負皆可）和必填的調整原因，調整後庫存不可小於 0。
    /// </summary>
    [HttpPost("adjust")]
    public async Task<ActionResult<InventoryItemDto>> Adjust([FromBody] AdjustInventoryRequest request, CancellationToken ct)
    {
        var result = await inventoryService.AdjustAsync(request, this.GetCurrentUserId(), this.GetCurrentUsername(), ct);
        return Ok(result);
    }
}
