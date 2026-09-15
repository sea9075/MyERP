using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// 薪資系統（新增：薪資系統）。只有 HR/Manager/Admin 能用；一般員工目前還看不到自己的薪資紀錄
/// （不在這次的需求範圍內，之後如果要開放，加一支 GET /api/payroll/me 就可以，不用動資料結構）。
/// </summary>
[ApiController]
[Route("api/payroll")]
[Authorize(Roles = "HR,Manager,Admin")]
public class PayrollController(IPayrollService payrollService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PayrollRecordDto>>> Search(
        [FromQuery] int? employeeId,
        [FromQuery] DateTime? periodMonth,
        [FromQuery] bool includeDeleted,
        CancellationToken ct) =>
        Ok(await payrollService.SearchAsync(employeeId, periodMonth, includeDeleted, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PayrollRecordDto>> GetById(int id, CancellationToken ct) =>
        Ok(await payrollService.GetByIdAsync(id, ct));

    /// <summary>依當月出勤紀錄計算薪資（含加班費）。已經算過的話會重算一次，見 PayrollService 的說明。</summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<PayrollRecordDto>> Calculate([FromBody] CalculatePayrollRequest request, CancellationToken ct)
    {
        var result = await payrollService.CalculateAsync(request, this.GetCurrentUsername(), ct);
        return Ok(result);
    }

    /// <summary>填入/修改獎金金額（人工輸入，沒有固定公式）。</summary>
    [HttpPut("{id:int}/bonus")]
    public async Task<ActionResult<PayrollRecordDto>> UpdateBonus(int id, [FromBody] UpdatePayrollBonusRequest request, CancellationToken ct) =>
        Ok(await payrollService.UpdateBonusAsync(id, request, this.GetCurrentUsername(), ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await payrollService.DeleteAsync(id, this.GetCurrentUsername(), ct);
        return NoContent();
    }
}
