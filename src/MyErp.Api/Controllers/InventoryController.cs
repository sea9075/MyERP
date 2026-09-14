using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    /// <summary>GET /api/inventory（ERP.md §4.5 / §6）：即時庫存列表。異動明細查詢／手動調整排到 Phase 2。</summary>
    [HttpGet]
    public async Task<ActionResult<List<InventoryItemDto>>> GetInventory(CancellationToken ct) =>
        Ok(await inventoryService.GetInventoryAsync(ct));
}
