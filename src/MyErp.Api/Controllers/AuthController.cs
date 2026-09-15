using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Api.Extensions;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>POST /api/auth/login（ERP.md §6）。帳密錯誤回 401，不區分是帳號不存在還是密碼錯，避免帳號列舉。</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        if (result is null)
        {
            return Unauthorized(new { message = "帳號或密碼錯誤。" });
        }

        return Ok(result);
    }

    /// <summary>
    /// 使用者自己修改自己的密碼，任何登入使用者都可以用（不分部門，含 HR 自己），對應右上角選單的
    /// 「密碼修改」（2026-09-15 新增）。需要先驗證目前密碼；跟 PUT /api/employees/{id}/password
    /// （HR/Manager/Admin 在員工管理裡強制重設別人的密碼，不需要舊密碼）是兩支不同的 API。
    /// </summary>
    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(this.GetCurrentUserId(), request, this.GetCurrentUsername(), ct);
        return NoContent();
    }
}
