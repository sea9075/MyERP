using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyErp.Application.Abstractions;
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

        services.AddDbContext<MyErpDbContext>(options => options.UseSqlServer(connectionString));

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

        return services;
    }
}
