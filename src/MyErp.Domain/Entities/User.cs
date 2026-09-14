using MyErp.Domain.Enums;

namespace MyErp.Domain.Entities;

/// <summary>
/// 使用者（ERP.md §5.1 User）。採用「自建 Users 表」而非完整 ASP.NET Core Identity，
/// 符合 ERP.md §2 認證欄位裡「JWT (ASP.NET Identity 精簡版或自建 Users 表)」的第二個選項，
/// 對只有 1~5 人使用的小系統來說更輕量。
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    /// <summary>PBKDF2 雜湊後的密碼（見 MyErp.Application.Common.PasswordHasher），絕不存明文。</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Staff;
    public bool IsActive { get; set; } = true;
}
