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
