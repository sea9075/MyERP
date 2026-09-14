using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>查詢「誰在什麼時候做了什麼」的操作紀錄。寫入是全域 Action Filter 自動做的，見 MyErp.Api.Filters.ActivityLogActionFilter。</summary>
[ApiController]
[Route("api/activity-logs")]
[Authorize]
public class ActivityLogsController(IActivityLogService activityLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ActivityLogDto>>> Search(
        [FromQuery] string? username,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct) =>
        Ok(await activityLogService.SearchAsync(username, dateFrom, dateTo, ct));
}
