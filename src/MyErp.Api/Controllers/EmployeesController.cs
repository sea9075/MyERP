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
}
