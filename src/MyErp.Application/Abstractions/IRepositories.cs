using MyErp.Domain.Entities;

namespace MyErp.Application.Abstractions;

// 這個檔案把 Phase 1/2/人資薪資 用到的所有 Repository 介面放在一起，方便一眼看到整體資料存取範圍。
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

    /// <summary>
    /// 檢查分類編號（Code）是否已經被其他「未刪除」分類使用。excludeId 用在更新時排除自己。
    /// </summary>
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken ct = default);

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

    /// <summary>一律 IgnoreQueryFilters()：EmployeeService 建立/修改員工時，用 Id 查詢不受刪除狀態影響。</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>檢查帳號是否已經被其他「未刪除」使用者使用（新增員工時檢查 Username 是否重複用）。</summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);

    void Add(User user);
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

/// <summary>員工人資資料（新增：人資系統）。</summary>
public interface IEmployeeRepository
{
    /// <summary>includeDeleted=true 時用 IgnoreQueryFilters() 連已離職（軟刪除）的員工都列出來。</summary>
    Task<List<Employee>> GetAllAsync(bool includeDeleted, CancellationToken ct = default);

    /// <summary>一律 IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>依登入帳號的 UserId 找員工資料，自助打卡（AttendanceService）用這個反查 EmployeeId。</summary>
    Task<Employee?> GetByUserIdAsync(int userId, CancellationToken ct = default);

    void Add(Employee employee);
}

/// <summary>每日出勤紀錄（新增：薪資/出勤系統）。</summary>
public interface IAttendanceRecordRepository
{
    Task<List<AttendanceRecord>> SearchAsync(int? employeeId, DateTime? dateFrom, DateTime? dateTo, bool includeDeleted, CancellationToken ct = default);

    /// <summary>一律 IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    Task<AttendanceRecord?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>找某位員工「某一天」未刪除的那筆紀錄——打卡（上班/下班）跟手動建立前都要先查有沒有重複。</summary>
    Task<AttendanceRecord?> GetByEmployeeAndDateAsync(int employeeId, DateTime workDate, CancellationToken ct = default);

    void Add(AttendanceRecord record);
}

/// <summary>薪資紀錄（新增：薪資系統）。</summary>
public interface IPayrollRecordRepository
{
    Task<List<PayrollRecord>> SearchAsync(int? employeeId, DateTime? periodMonth, bool includeDeleted, CancellationToken ct = default);

    /// <summary>一律 IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    Task<PayrollRecord?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>找某位員工「某個月份」未刪除的那筆紀錄——重新計算薪資前，要先把舊的軟刪除掉。</summary>
    Task<PayrollRecord?> GetByEmployeeAndMonthAsync(int employeeId, DateTime periodMonth, CancellationToken ct = default);

    void Add(PayrollRecord record);
}

/// <summary>
/// 系統通知（2026-09-15 新增：worker 低庫存自動通知，見 MyErp.Domain.Entities.Notification 的說明）。
/// 這張表沒有稽核欄位／軟刪除，跟 ActivityLog 一樣是系統紀錄，但多了 IsRead 這個可變欄位。
/// </summary>
public interface INotificationRepository
{
    void Add(Notification notification);

    /// <summary>
    /// 檢查某個商品目前是否已經有一筆「未讀」的低庫存通知——worker（IInventoryEventHandler）
    /// 用這個做去重，避免同一個商品持續低於安全庫存時，每次庫存異動都重複寫入通知
    /// （使用者把這筆通知標記已讀之後，如果庫存還是低，下一次庫存減少的異動才會再產生新的通知）。
    /// </summary>
    Task<bool> HasUnreadLowStockAsync(int productId, CancellationToken ct = default);

    /// <summary>依時間新到舊排序；unreadOnly=true 時只回傳尚未標記已讀的通知。</summary>
    Task<List<Notification>> GetAllAsync(bool unreadOnly, CancellationToken ct = default);

    Task<int> CountUnreadAsync(CancellationToken ct = default);

    Task<Notification?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>直接在資料庫端批次更新（ExecuteUpdateAsync），不用先把整批通知讀進記憶體逐筆改。</summary>
    Task MarkAllReadAsync(CancellationToken ct = default);
}
