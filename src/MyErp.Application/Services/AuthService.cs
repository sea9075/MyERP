using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;

namespace MyErp.Application.Services;

public interface IAuthService
{
    /// <summary>帳密驗證成功則回傳登入結果（含 JWT），失敗回傳 null（Controller 轉成 401）。</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService(IUserRepository userRepository, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // UserRepository.GetByUsernameAsync 已經套用 Global Query Filter，已刪除(IsDeleted=true)
        // 的帳號本來就查不到，這裡不用再另外檢查一次。
        // 帳號修剪頭尾空白（使用者可能不小心打成 "admin "）；密碼刻意不修剪，因為密碼本身允許
        // 包含空白字元，修剪反而會讓使用者原本設定的密碼變得驗證不過。
        var user = await userRepository.GetByUsernameAsync(request.Username.TrimRequired(), ct);
        if (user is null)
        {
            return null;
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwt.ExpiresInMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            // 這裡的 ClaimTypes.Role 存的是 Department（部門＝權限角色，見 Domain.Enums.Department），
            // 沿用 ASP.NET Core 內建的 ClaimTypes.Role，是為了讓 [Authorize(Roles = "...")] 這個
            // 內建機制可以直接拿來用，不用另外寫一套部門比對邏輯。
            new Claim(ClaimTypes.Role, user.Department.ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Department.ToString(),
        };
    }
}
