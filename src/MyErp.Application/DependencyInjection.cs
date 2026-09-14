using Microsoft.Extensions.DependencyInjection;
using MyErp.Application.Services;

namespace MyErp.Application;

public static class DependencyInjection
{
    /// <summary>在 MyErp.Api/Program.cs 呼叫，註冊 Phase 1 用到的所有 Service。</summary>
    public static IServiceCollection AddMyErpApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }
}
