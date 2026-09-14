using MyErp.Domain.Entities;

namespace MyErp.Application.Abstractions;

// 這個檔案把 Phase 1 用到的所有 Repository 介面放在一起，方便一眼看到整體資料存取範圍。
// 實作（用 EF Core 操作 Azure SQL Database）都在 MyErp.Infra/Repositories/ 底下，一個介面對一個實作檔。

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> HasProductsAsync(int categoryId, CancellationToken ct = default);
    void Add(Category category);
    void Remove(Category category);
}

public interface ISupplierRepository
{
    Task<List<Supplier>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    void Add(Supplier supplier);
}

public interface ICustomerRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}

public interface IProductRepository
{
    /// <summary>依 ERP.md §6：GET /api/products?keyword=&amp;categoryId=&amp;lowStock=。停用商品預設不列出。</summary>
    Task<List<Product>> SearchAsync(string? keyword, int? categoryId, bool? lowStock, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null, CancellationToken ct = default);
    Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null, CancellationToken ct = default);
    void Add(Product product);
}

public interface IPurchaseOrderRepository
{
    Task<List<PurchaseOrder>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, int? supplierId, CancellationToken ct = default);
    Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>今天（依 UTC 日期字串 yyyyMMdd）已經有幾張進貨單，用來組出 PI-20260908-001 這種流水號。</summary>
    Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken ct = default);

    void Add(PurchaseOrder order);
}

public interface ISalesOrderRepository
{
    Task<List<SalesOrder>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, int? customerId, CancellationToken ct = default);
    Task<SalesOrder?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken ct = default);
    void Add(SalesOrder order);
}

public interface IInventoryTransactionRepository
{
    void Add(InventoryTransaction transaction);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
}
