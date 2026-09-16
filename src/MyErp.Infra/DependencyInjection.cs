using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyErp.Application.Abstractions;
using MyErp.Infra.Messaging;
using MyErp.Infra.Repositories;

namespace MyErp.Infra;

public static class DependencyInjection
{
    /// <summary>在 MyErp.Api/Program.cs 呼叫，註冊 DbContext（連 Azure SQL Database）與所有 Repository。</summary>
    public static IServiceCollection AddMyErpInfra(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "找不到 ConnectionStrings:DefaultConnection。請先執行 " +
                "`dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\"`（見交付說明）。");

        services.AddDbContext<MyErpDbContext>(options => options.UseSqlServer(
            connectionString,
            // Azure SQL Database（尤其 Serverless 方案）閒置一段時間會自動暫停，
            // 重新連線時第一個請求常會撞到「還在喚醒中」的暫時性錯誤（SQL 40613 等）。
            // EnableRetryOnFailure 讓 EF Core 遇到這類已知的暫時性錯誤時自動重試，
            // 不用每次都要手動重跑指令或重整頁面。
            //
            // 2026-09-16 補上 11002（見 MyERP-gitops 部署時實際踩到的坑，Infra-Progress.md
            // 有記錄）：K8s 叢集裡的 CoreDNS 在節點資源緊繃時會偶發不穩定，這個 Pod 嘗試解析
            // Azure SQL 網域名稱時如果剛好撞上，.NET 會丟出 SqlException（Error Number
            // 11002＝Windows socket 的 WSATRY_AGAIN，DNS 查詢當下沒拿到回應），但這個錯誤碼
            // 不在 EF Core 內建的「已知暫時性錯誤」清單裡，預設完全不會重試、直接讓整個
            // Migration／啟動流程炸掉。額外把它加進 errorNumbersToAdd，讓這種一次性的 DNS
            // 抖動也能被重試機制接住，不用每次都靠重啟 Pod 賭下一次 CoreDNS 剛好正常。
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: new[] { 11002 })));

        // MyErpDbContext 本身就實作 IUnitOfWork，直接轉接過去即可，不用另外包一個 class。
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MyErpDbContext>());

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

        // 新增：人資/薪資/出勤系統
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<IPayrollRecordRepository, PayrollRecordRepository>();

        // 新增：worker 低庫存自動通知（2026-09-15，見 Infra-Progress.md §31）。
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // ServiceBusClient 官方建議整個應用程式共用一個單例，本身是 thread-safe 的。
        // 本機開發如果還沒設定 ServiceBus:ConnectionString（例如剛 clone 專案、還沒跑
        // dotnet user-secrets set），就退回 NullEventPublisher，不要讓整個 API／單元測試開不起來
        // ——事件發布本來就是 best-effort，見 SalesOrderService／PurchaseOrderService 呼叫端的說明。
        var serviceBusConnectionString = configuration["ServiceBus:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
            services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
        }
        else
        {
            services.AddScoped<IEventPublisher, NullEventPublisher>();
        }

        return services;
    }
}
