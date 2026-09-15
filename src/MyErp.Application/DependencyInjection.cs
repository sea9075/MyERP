using Microsoft.Extensions.DependencyInjection;
using MyErp.Application.Services;

namespace MyErp.Application;

public static class DependencyInjection
{
    /// <summary>在 MyErp.Api/Program.cs 呼叫，註冊所有 Service。</summary>
    public static IServiceCollection AddMyErpApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();

        // 新增：人資/薪資/出勤系統
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPayrollService, PayrollService>();

        return services;
    }
}
