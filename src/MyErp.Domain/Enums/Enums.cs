namespace MyErp.Domain.Enums;

/// <summary>
/// 部門，同時也是系統的權限角色（新增：人資/薪資/權限系統）。
/// Product＝一般 ERP 操作人員（商品/供應商/客戶/進出貨/庫存），HR＝人資與薪資，
/// Manager／Admin＝管理層，兩者權限目前完全相同（系統裡還沒有「只有 Admin 能做、Manager 不行」的功能，
/// 之後如果真的出現這種需求，再回來對個別 API 額外加 "Admin" 限制即可）。
///
/// 取代原本的 UserRole（Admin/Staff）：原本 Admin=0、Staff=1，這兩個數值刻意保留給
/// Product=0、（原 Admin 對應到）Admin=3，實際的新舊資料對應寫在遷移腳本的手動修正裡
/// （見交付說明「重要：套用 migration 前要做的事」）。
/// </summary>
public enum Department
{
    Product = 0,
    HR = 1,
    Manager = 2,
    Admin = 3,

    /// <summary>
    /// 客服部門（新增）。只能用客戶管理（CustomersController）跟出貨單（SalesOrdersController）；
    /// 因為新增出貨單時要從商品下拉選單選商品，所以額外開放 ProductsController 的「唯讀」動作
    /// （查詢/依 Id/依條碼），但不能新增/修改/刪除商品，也看不到「商品管理」畫面。
    /// </summary>
    Support = 4
}

/// <summary>
/// 進貨單／銷售單的狀態。對應 ERP.md §5：0=正常, 1=已作廢。
/// （作廢/沖銷邏輯屬於 Phase 2，這個 enum 先定義好，Phase 1 只會用到 Normal。）
/// </summary>
public enum OrderStatus
{
    Normal = 0,
    Voided = 1
}

/// <summary>
/// 庫存異動類型。對應 ERP.md §5 InventoryTransaction.ChangeType。
/// Phase 1 只會寫入 Purchase / Sale，ManualAdjustment 與兩種 Void 保留給 Phase 2。
/// </summary>
public enum InventoryChangeType
{
    Purchase = 1,
    Sale = 2,
    ManualAdjustment = 3,
    PurchaseVoid = 4,
    SaleVoid = 5
}

/// <summary>
/// 出勤紀錄的來源（新增：人資/薪資系統）。SelfService＝員工自己登入系統打卡；
/// ManualEntry＝HR/Manager/Admin 事後手動建立或補登（例如忘記打卡、系統問題）。
/// </summary>
public enum AttendanceSource
{
    SelfService = 0,
    ManualEntry = 1
}
