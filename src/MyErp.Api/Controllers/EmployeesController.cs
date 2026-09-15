using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>人資系統：員工資料 CRUD（新增：人資系統）。只有 HR/Manager/Admin 能用，Product 部門完全看不到。</summary>
[ApiController]
[Route("api/employees")]
[Authorize(Roles = "HR,Manager,Admin")]
public class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll([FromQuery] bool includeDeleted, CancellationToken ct) =>
        Ok(await employeeService.GetAllAsync(includeDeleted, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id, CancellationToken ct) =>
        Ok(await employeeService.GetByIdAsync(id, ct));

    /// <summary>新增員工＝同時開一組登入帳號，見 EmployeeService.CreateAsync 的說明。</summary>
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var created = await employeeService.CreateAsync(request, this.GetCurrentUsername(), ct);
        return Ok(created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct) =>
        Ok(await employeeService.UpdateAsync(id, request, this.GetCurrentUsername(), ct));

    /// <summary>軟刪除＝離職。</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await employeeService.DeleteAsync(id, this.GetCurrentUsername(), ct);
        return NoContent();
    }

    /// <summary>
    /// HR/Manager/Admin 重設員工密碼（2026-09-15 新增，使用者決定：跟這個 Controller 其餘操作
    /// 一樣的權限範圍，不特別把 Manager/Admin 排除在外）。不需要驗證舊密碼，沿用 class 上的
    /// [Authorize(Roles = "HR,Manager,Admin")]，不用額外加限制。
    /// </summary>
    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetEmployeePasswordRequest request, CancellationToken ct)
    {
        await employeeService.ResetPasswordAsync(id, request, this.GetCurrentUsername(), ct);
        return NoContent();
    }
}
