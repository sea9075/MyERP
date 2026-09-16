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
        services.AddScoped<IReportService, ReportService>();

        // 新增：人資/薪資/出勤系統
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPayrollService, PayrollService>();

        // 新增：worker 低庫存自動通知（2026-09-15，見 Infra-Progress.md §31）。
        // INotificationService 給 MyErp.Api（通知列表 API）用；IInventoryEventHandler 給
        // MyErp.Worker（訂閱 Service Bus）用，兩者都註冊在這裡，因為都是 Application 層服務。
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IInventoryEventHandler, InventoryEventHandler>();

        return services;
    }
}
