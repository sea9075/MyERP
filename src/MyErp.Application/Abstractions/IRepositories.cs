using MyErp.Domain.Entities;

namespace MyErp.Application.Abstractions;

// 這個檔案把 Phase 1/2 用到的所有 Repository 介面放在一起，方便一眼看到整體資料存取範圍。
// 實作（用 EF Core 操作 Azure SQL Database）都在 MyErp.Infra/Repositories/ 底下，一個介面對一個實作檔。
//
// 這次（稽核欄位 + 全面軟刪除）之後，Category/Product/Supplier/Customer 都不再有 Remove()，
// 因為刪除一律改成軟刪除（設 IsDeleted=true），不再有實體刪除。

public interface ICategoryRepository
{
    /// <summary>includeDeleted=true 時會用 IgnoreQueryFilters() 連已軟刪除的分類都列出來。</summary>
    Task<List<Category>> GetAllAsync(bool includeDeleted, CancellationToken ct = default);

    /// <summary>一律 IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>只算「未刪除」的商品（Product 的 Global Query Filter 自動套用）。</summary>
    Task<bool> HasProductsAsync(int categoryId, CancellationToken ct = default);

    /// <summary>
    /// 檢查名稱是否已經被其他「未刪除」分類使用（Global Query Filter 自動套用，
    /// 已軟刪除的分類不會擋新分類使用同樣的名稱）。excludeId 用在更新時排除自己。
    /// </summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);

    void Add(Category category);
}

public interface ISupplierRepository
{
    Task<List<Supplier>> GetAllAsync(bool includeDeleted, CancellationToken ct = default);
    Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>檢查名稱是否已經被其他「未刪除」供應商使用。excludeId 用在更新時排除自己。</summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);

    void Add(Supplier supplier);
}

public interface ICustomerRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<List<Customer>> GetAllAsync(bool includeDeleted, CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>SalesOrder 沒有套用軟刪除／Global Query Filter，這裡會查到「所有」出貨單（含已作廢的）。</summary>
    Task<bool> HasSalesOrdersAsync(int customerId, CancellationToken ct = default);

    /// <summary>檢查名稱是否已經被其他「未刪除」客戶使用。excludeId 用在更新時排除自己。</summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);

    void Add(Customer customer);
}

public interface IProductRepository
{
    /// <summary>
    /// 依 ERP.md §6：GET /api/products?keyword=&amp;categoryId=&amp;lowStock=&amp;includeDeleted=。
    /// includeDeleted=true 時用 IgnoreQueryFilters() 連已軟刪除的商品都列出來。
    /// </summary>
    Task<List<Product>> SearchAsync(string? keyword, int? categoryId, bool? lowStock, bool includeDeleted, CancellationToken ct = default);

    /// <summary>一律 IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響（例如作廢舊單據時仍要找得到已刪除的商品）。</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>條碼掃描查詢：只找「未刪除」的商品（新交易不該掃到已刪除的商品）。</summary>
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

    /// <summary>Phase 2（ERP.md §8 項目 9）：GET /api/inventory/{productId}/transactions 用，依時間新到舊排序。</summary>
    Task<List<InventoryTransaction>> GetByProductIdAsync(int productId, CancellationToken ct = default);
}

public interface IUserRepository
{
    /// <summary>只會找到「未刪除」的使用者（Global Query Filter 自動套用）。</summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
}

/// <summary>
/// 操作紀錄（誰在什麼時候做了什麼）的資料存取。這張表本身沒有稽核欄位／軟刪除，
/// 是不可變的日誌，見 MyErp.Domain.Entities.ActivityLog 的說明。
/// </summary>
public interface IActivityLogRepository
{
    void Add(ActivityLog log);

    Task<List<ActivityLog>> SearchAsync(string? username, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
}
