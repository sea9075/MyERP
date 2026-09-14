using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class UserRepository(MyErpDbContext db) : IUserRepository
{
    /// <summary>
    /// 不用另外寫 `&amp;&amp; !u.IsDeleted`：User 的 Global Query Filter 已經自動排除已刪除的使用者，
    /// 被刪除的帳號查不到，等同登入會失敗（見 AuthService.LoginAsync）。
    /// </summary>
    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
}
