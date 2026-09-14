using Microsoft.EntityFrameworkCore;
using MyErp.Application.Common;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;

namespace MyErp.Infra.Seed;

/// <summary>
/// 系統第一次啟動時，Users 表是空的，沒有人可以登入，所以需要一組預設帳號。
/// 這裡故意不用 EF Core Migration 的 HasData（那個是「設計期」就要把雜湊後密碼寫死進
/// migration 檔案，之後要換密碼還要重新產生 migration），改成在 Program.cs 啟動時
/// 用一般的「檢查有沒有資料、沒有就寫入」的方式跑，比較彈性也比較直觀。
///
/// ⚠️ 帳密是明文寫在程式碼裡的預設值，僅供第一次登入使用，正式使用前務必自行改密碼
/// （目前 Phase 1 還沒有「修改密碼」的 API，這點會在文件裡明確跟你說明）。
/// </summary>
public static class SeedData
{
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin@123456";

    public static async Task SeedAsync(MyErpDbContext db, CancellationToken ct = default)
    {
        var hasAnyUser = await db.Users.AnyAsync(ct);
        if (hasAnyUser)
        {
            return;
        }

        db.Users.Add(new User
        {
            Username = DefaultAdminUsername,
            PasswordHash = PasswordHasher.Hash(DefaultAdminPassword),
            DisplayName = "系統管理員",
            Role = UserRole.Admin,
            IsActive = true,
        });

        await db.SaveChangesAsync(ct);
    }
}
