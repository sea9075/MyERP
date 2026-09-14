namespace MyErp.Domain.Enums;

/// <summary>
/// 使用者角色。對應 ERP.md §4.7：
/// Admin = 全功能（含商品/供應商設定、報表）；Staff = 僅進出貨操作、庫存查詢。
/// </summary>
public enum UserRole
{
    Admin = 0,
    Staff = 1
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
