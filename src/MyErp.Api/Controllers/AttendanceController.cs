using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// 出勤系統（新增：薪資/出勤系統）。打卡（clock-in/clock-out）與查自己的紀錄（me）任何登入使用者
/// 都可以用，不分部門——因為不管哪個部門，員工都要打卡；HR 幫別人手動建立/查全部/修改/刪除
/// 這幾支才限定 HR/Manager/Admin，見各個 Action 上的 [Authorize(Roles = ...)]。
/// </summary>
[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    /// <summary>員工自己打上班卡。</summary>
    [HttpPost("clock-in")]
    public async Task<ActionResult<AttendanceRecordDto>> ClockIn([FromBody] ClockInRequest request, CancellationToken ct)
    {
        var result = await attendanceService.ClockInAsync(this.GetCurrentUserId(), request, this.GetCurrentUsername(), ct);
        return Ok(result);
    }

    /// <summary>員工自己打下班卡。</summary>
    [HttpPost("clock-out")]
    public async Task<ActionResult<AttendanceRecordDto>> ClockOut([FromBody] ClockOutRequest request, CancellationToken ct)
    {
        var result = await attendanceService.ClockOutAsync(this.GetCurrentUserId(), request, this.GetCurrentUsername(), ct);
        return Ok(result);
    }

    /// <summary>員工查自己的出勤紀錄，任何登入使用者都可以用。</summary>
    [HttpGet("me")]
    public async Task<ActionResult<List<AttendanceRecordDto>>> GetMine(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct) =>
        Ok(await attendanceService.GetMyRecordsAsync(this.GetCurrentUserId(), dateFrom, dateTo, ct));

    /// <summary>查所有員工的出勤紀錄，只有 HR/Manager/Admin 能用。</summary>
    [HttpGet]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<ActionResult<List<AttendanceRecordDto>>> Search(
        [FromQuery] int? employeeId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] bool includeDeleted,
        CancellationToken ct) =>
        Ok(await attendanceService.SearchAsync(employeeId, dateFrom, dateTo, includeDeleted, ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<ActionResult<AttendanceRecordDto>> GetById(int id, CancellationToken ct) =>
        Ok(await attendanceService.GetByIdAsync(id, ct));

    /// <summary>HR/Manager/Admin 幫員工手動建立/補登一筆出勤紀錄。</summary>
    [HttpPost("manual")]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<ActionResult<AttendanceRecordDto>> CreateManual([FromBody] CreateManualAttendanceRequest request, CancellationToken ct)
    {
        var created = await attendanceService.CreateManualAsync(request, this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    /// <summary>HR/Manager/Admin 修改一筆出勤紀錄（不論原本是自助打卡還是手動建立的）。</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<ActionResult<AttendanceRecordDto>> Update(int id, [FromBody] UpdateManualAttendanceRequest request, CancellationToken ct) =>
        Ok(await attendanceService.UpdateAsync(id, request, this.GetCurrentUsername(), ct));

    /// <summary>軟刪除，HR 打錯資料要能刪掉重建。</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await attendanceService.DeleteAsync(id, this.GetCurrentUsername(), ct);
        return NoContent();
    }
}
