using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
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
        // 這也是 Product/Supplier 用 IsActive 軟刪除、Category 刪除前要先檢查底下有沒有商品的原因。
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ContactPerson).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.Note).HasMaxLength(200);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Note).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Sku).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Barcode).HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(10).IsRequired();
            entity.Property(e => e.CostPrice).HasPrecision(10, 2);
            entity.Property(e => e.SalePrice).HasPrecision(10, 2);

            entity.HasIndex(e => e.Sku).IsUnique();

            // Barcode 可為空但若有值必須唯一：SQL Server 用「篩選式唯一索引」排除 NULL，
            // 否則多筆都是 NULL 的商品會被唯一索引擋下來（NULL 在一般唯一索引裡也會被視為互斥）。
            entity.HasIndex(e => e.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");

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
            entity.HasIndex(e => e.OrderNo).IsUnique();

            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(e => e.SupplierId);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId);
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
            entity.HasIndex(e => e.OrderNo).IsUnique();

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.SalesOrders)
                .HasForeignKey(e => e.CustomerId);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId);
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
    }
}
