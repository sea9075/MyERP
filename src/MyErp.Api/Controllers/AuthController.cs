using Microsoft.AspNetCore.Mvc;
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
}
