using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// "Product" / "HR" / "Manager" / "Admin"，對應 Domain.Enums.Department（部門＝權限角色）。
    /// 欄位名稱沿用舊的 "Role"（沒有改成 "Department"），是為了不用同時改前端的欄位名稱；
    /// 內容意義已經從「UserRole」變成「Department」，前端讀到的字串值也不一樣了（不再只有
    /// "Admin"/"Staff" 兩種），這點麻煩前端這邊之後配合更新權限判斷邏輯。
    /// </summary>
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// 使用者自助修改自己的密碼（右上角選單「密碼修改」，任何登入使用者都可以用，不分部門）。
/// 需要先驗證目前密碼，跟 HR 在員工管理裡強制重設密碼（不需要舊密碼）不同，見 EmployeeDtos.cs 的
/// ResetEmployeePasswordRequest 說明。
/// </summary>
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "密碼至少需要 8 個字元")]
    public string NewPassword { get; set; } = string.Empty;
}
