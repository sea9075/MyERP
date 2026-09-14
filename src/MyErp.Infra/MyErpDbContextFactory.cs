using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MyErp.Infra;

/// <summary>
/// 給 `dotnet ef migrations add` / `dotnet ef database update` 這類「設計期」CLI 工具用的工廠。
/// 這些指令不會啟動整個 ASP.NET Core Host（Program.cs 不會執行），所以連線字串沒辦法用
/// 平常 DI 注入的方式取得，要在這裡自己組一份最小設定，直接讀 MyErp.Api 專案底下的
/// user-secrets（透過下面寫死的 UserSecretsId）去拿 ConnectionStrings:DefaultConnection。
///
/// 注意：這個寫死的 GUID 必須和 src/MyErp.Api/MyErp.Api.csproj 裡的
/// &lt;UserSecretsId&gt; 完全一致，兩邊才會讀到同一份 secrets.json。
/// </summary>
public class MyErpDbContextFactory : IDesignTimeDbContextFactory<MyErpDbContext>
{
    public const string UserSecretsId = "a3f1e9d2-6c4b-4a7e-9f0d-1b2c3d4e5f6a";

    public MyErpDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(UserSecretsId)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "找不到 ConnectionStrings:DefaultConnection。請先在 src/MyErp.Api 執行 " +
                "`dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\"`。");

        var optionsBuilder = new DbContextOptionsBuilder<MyErpDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new MyErpDbContext(optionsBuilder.Options);
    }
}
