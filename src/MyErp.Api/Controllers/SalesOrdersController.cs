using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

[ApiController]
[Route("api/sales-orders")]
[Authorize]
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
        var currentUserId = GetCurrentUserId();
        var created = await salesOrderService.CreateAsync(request, currentUserId, ct);
        return Ok(created);
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("JWT 缺少使用者 Id (NameIdentifier claim)，這裡不應該發生。");
        return int.Parse(idClaim);
    }
}
