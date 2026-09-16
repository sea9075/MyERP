using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// 系統通知（2026-09-15 新增：worker 低庫存自動通知，見 Infra-Progress.md §31）。
/// 跟商品/庫存屬於同一個工作範圍，開放對象比照辦理：Product/Manager/Admin。
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "Product,Manager,Admin")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetAll([FromQuery] bool unreadOnly, CancellationToken ct) =>
        Ok(await notificationService.GetAllAsync(unreadOnly, ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> CountUnread(CancellationToken ct) =>
        Ok(await notificationService.CountUnreadAsync(ct));

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await notificationService.MarkReadAsync(id, ct);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await notificationService.MarkAllReadAsync(ct);
        return NoContent();
    }
}
