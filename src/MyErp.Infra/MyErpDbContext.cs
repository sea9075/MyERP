using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyErp.Application.Abstractions;
using MyErp.Domain.Common;
using MyErp.Domain.Entities;

namespace MyErp.Infra;

/// <summary>
/// 這個系統唯一的 EF Core DbContext。直接實作 IUnitOfWork——DbContext 本來就是
/// Unit of Work 的實作，沒必要再包一層一模一樣的 class。
/// </summary>
public class MyErpDbContext(DbContextOptions<MyErpDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    // 新增：人資/薪資/出勤系統
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            await operation();
            await transaction.CommitAsync(cancellationToken);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 整個檔案的外鍵一律設成 Restrict：這是一套會計/稽核性質的進出貨系統，
        // 不希望刪除一筆「上游」資料（例如商品、供應商）時，順便把歷史單據/庫存異動紀錄也砍掉。
        // 這也是 Product/Supplier/Category/Customer/User 全部改用 IsDeleted 軟刪除的原因之一。
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(5).IsRequired();
            ConfigureAuditColumns(entity);

            // 只在「未刪除」的分類之間唯一：分類刪除後，同樣的編號可以再被新分類使用。
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ContactPerson).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.Note).HasMaxLength(200);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Note).HasMaxLength(200);
            ConfigureAuditColumns(entity);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(200).IsRequired();
            ConfigureAuditColumns(entity);

            // 只在「未刪除」的使用者之間唯一：帳號被刪除後，同樣的 username 可以再開一個新帳號。
            entity.HasIndex(e => e.Username).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Sku).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Barcode).HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(10).IsRequired();
            entity.Property(e => e.CostPrice).HasPrecision(10, 2);
            entity.Property(e => e.SalePrice).HasPrecision(10, 2);
            ConfigureAuditColumns(entity);

            // 只在「未刪除」的商品之間唯一，刪除後可以讓新商品重複使用同一個 SKU/條碼。
            entity.HasIndex(e => e.Sku).IsUnique().HasFilter("[IsDeleted] = 0");

            // Barcode 可為空但若有值必須唯一：SQL Server 用「篩選式唯一索引」排除 NULL 跟已刪除的商品，
            // 否則多筆都是 NULL 的商品會被唯一索引擋下來（NULL 在一般唯一索引裡也會被視為互斥）。
            entity.HasIndex(e => e.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId);

            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(e => e.SupplierId);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.Property(e => e.OrderNo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.OrderNo).IsUnique();

            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(e => e.SupplierId);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.Subtotal).HasPrecision(10, 2);

            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.PurchaseOrderId);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.Property(e => e.OrderNo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.OrderNo).IsUnique();

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.SalesOrders)
                .HasForeignKey(e => e.CustomerId);
        });

        modelBuilder.Entity<SalesOrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.Subtotal).HasPrecision(10, 2);

            entity.HasOne(e => e.SalesOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.SalesOrderId);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.SalesOrderItems)
                .HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.Property(e => e.RefTable).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(200);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(e => e.ProductId);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId);
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.Property(e => e.Api).HasMaxLength(300).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsRequired();
        });

        // ---- 新增：人資/薪資/出勤系統 ----

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.MonthlySalary).HasPrecision(10, 2);
            entity.Property(e => e.JobTitle).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(50);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(30);
            ConfigureAuditColumns(entity);

            // 一個 User 最多只能對應一筆 Employee（一對一）。
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.Property(e => e.Note).HasMaxLength(200);
            ConfigureAuditColumns(entity);

            // 一位員工、一天只能有一筆「未刪除」的出勤紀錄；刪除後可以讓同一天重新補登一筆。
            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate }).IsUnique().HasFilter("[IsDeleted] = 0");

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.AttendanceRecords)
                .HasForeignKey(e => e.EmployeeId);
        });

        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.Property(e => e.BaseSalary).HasPrecision(10, 2);
            entity.Property(e => e.RegularHours).HasPrecision(6, 2);
            entity.Property(e => e.OvertimeHoursTier1).HasPrecision(6, 2);
            entity.Property(e => e.OvertimeHoursTier2).HasPrecision(6, 2);
            entity.Property(e => e.OvertimePay).HasPrecision(10, 2);
            entity.Property(e => e.BonusAmount).HasPrecision(10, 2);
            entity.Property(e => e.TotalPay).HasPrecision(10, 2);
            entity.Property(e => e.Note).HasMaxLength(200);
            ConfigureAuditColumns(entity);

            // 一個員工、一個月份只能有一筆「未刪除」的薪資紀錄；重新計算會先軟刪除舊的那一筆。
            entity.HasIndex(e => new { e.EmployeeId, e.PeriodMonth }).IsUnique().HasFilter("[IsDeleted] = 0");

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.PayrollRecords)
                .HasForeignKey(e => e.EmployeeId);
        });

        // ---- 全站時區修正 ----
        // 問題（使用者回報）：新增進貨單當下畫面顯示的時間，跟列表重新整理後顯示的時間差了 8 小時
        // （台灣 UTC+8）。根本原因：所有 DateTime 欄位存進 SQL Server datetime2 時雖然存的是 UTC
        // 時間（程式裡都用 DateTime.UtcNow／前端 toISOString()），但 SQL Server 不記錄時區資訊，
        // EF Core 讀回來的 CLR DateTime.Kind 一律是 Unspecified；System.Text.Json 序列化
        // Unspecified 的 DateTime 時不會加上代表 UTC 的 "Z" 尾碼，前端 dayjs() 收到沒有 "Z" 的字串
        // 會誤判成「已經是瀏覽器當地時間」，等於整個時間被少轉換一次時區，正好差 8 小時。
        //
        // 修法：在讀出資料庫的當下，把每一個 DateTime/DateTime? 欄位的 Kind 一律標記成 Utc
        // （寫入資料庫時原樣寫回，不做任何轉換——反正存的本來就已經是 UTC 時間）。這樣序列化出去
        // 一律會帶 "Z"，前端 dayjs() 才能正確轉換成瀏覽器當地時間顯示，全站每一個 DateTime 欄位都
        // 受惠，不用一個一個欄位改。
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                }
            }
        }

        // 對所有實作 IAuditable 的實體（Category/Product/Supplier/Customer/User/Employee/
        // AttendanceRecord/PayrollRecord）自動套用 Global Query Filter：預設查詢一律排除
        // IsDeleted=true 的資料。個別 Repository 需要看到已刪除資料時（例如 includeDeleted=true、
        // 或 GetByIdAsync 這種不受刪除狀態影響的查詢），會用 IgnoreQueryFilters() 明確繞過。
        //
        // 注意：這個過濾器也會套用到 Include() 帶出來的關聯物件——例如某個 Customer 被軟刪除後，
        // 舊的 SalesOrder.Include(o => o.Customer) 會讀到 null（因為 SalesOrder 本身沒有套用軟刪除，
        // 不會消失，但它關聯的 Customer 因為篩選器的關係讀不到）。這是刻意接受的取捨：如果之後想讓
        // 歷史單據仍然顯示已刪除客戶的名字，可以另外在 SalesOrderDto 存一份快照欄位，這次先不做。
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProperty = Expression.Property(parameter, nameof(IAuditable.IsDeleted));
            var notDeleted = Expression.Not(isDeletedProperty);
            var lambda = Expression.Lambda(notDeleted, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>幫實作 IAuditable 的實體統一設定稽核欄位的型別/長度限制，避免多張表重複寫一樣的設定。</summary>
    private static void ConfigureAuditColumns<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IAuditable
    {
        entity.Property(e => e.CreatedBy).HasMaxLength(50).IsRequired();
        entity.Property(e => e.UpdatedBy).HasMaxLength(50).IsRequired();
    }
}
