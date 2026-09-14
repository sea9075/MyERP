using MyErp.Domain.Common;
using MyErp.Domain.Enums;

namespace MyErp.Domain.Entities;

/// <summary>
/// 使用者（ERP.md §5.1 User）。採用「自建 Users 表」而非完整 ASP.NET Core Identity，
/// 符合 ERP.md §2 認證欄位裡「JWT (ASP.NET Identity 精簡版或自建 Users 表)」的第二個選項，
/// 對只有 1~5 人使用的小系統來說更輕量。
///
/// Username 一旦建立就不能更改（業務規則，目前 Phase 1/2 也還沒有「修改使用者」的 API），
/// 所以其他資料表的 CreatedBy/UpdatedBy 才能安心直接存 Username 字串快照，不用擔心之後改名。
/// </summary>
public class User : IAuditable
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    /// <summary>PBKDF2 雜湊後的密碼（見 MyErp.Application.Common.PasswordHasher），絕不存明文。</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Staff;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>是否已刪除（軟刪除，原本叫 IsActive，這次統一改名成 IsDeleted 並反轉語意）。</summary>
    public bool IsDeleted { get; set; }
}
