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

    /// <summary>一律 IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響（EmployeeService 建立/修改員工用）。</summary>
    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <summary>只算「未刪除」的帳號（Global Query Filter 自動套用），刪除後的帳號可以讓新員工重複使用同樣的 username。</summary>
    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Username == username, ct);

    public void Add(User user) => db.Users.Add(user);
}
