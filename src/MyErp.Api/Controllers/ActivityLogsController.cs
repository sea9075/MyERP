using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// 查詢「誰在什麼時候做了什麼」的操作紀錄。寫入是全域 Action Filter 自動做的，見 MyErp.Api.Filters.ActivityLogActionFilter。
/// 只有 Manager/Admin 能看（新增：人資/薪資/權限系統，之前是任何登入使用者都能看，這次依需求收緊）。
/// </summary>
[ApiController]
[Route("api/activity-logs")]
[Authorize(Roles = "Manager,Admin")]
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
