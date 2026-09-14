namespace MyErp.Domain.Common;

/// <summary>
/// 稽核欄位共用介面（不含 IsDeleted）：createdAt/updatedAt/createdBy/updatedBy。
/// PurchaseOrder/SalesOrder 用這個介面——這兩張表已經有「作廢(Void)」機制當作狀態管理，
/// 不需要（也刻意不加）isDeleted。
///
/// createdBy/updatedBy 存的是 User.Username 的字串快照，不是外鍵。因為 Username 一旦建立
/// 就不能更改（業務規則），直接存字串既簡單、也不會有之後帳號改名或被刪除導致歷史紀錄跟著跑掉的問題。
/// </summary>
public interface ITrackable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    string CreatedBy { get; set; }
    string UpdatedBy { get; set; }
}

/// <summary>
/// 完整稽核欄位介面：在 <see cref="ITrackable"/> 之上多加 IsDeleted，代表這個實體採用軟刪除。
/// 套用在 Category / Product / Supplier / Customer / User 這 5 個主檔。
///
/// 軟刪除只把 IsDeleted 設成 true、同時更新 UpdatedAt/UpdatedBy，刻意不另外設計 DeletedAt/DeletedBy
/// 欄位——查詢「誰在什麼時候刪除的」，直接看 UpdatedAt/UpdatedBy 就好，因為刪除本身也是一種「異動」。
///
/// MyErpDbContext 會對所有實作這個介面的實體自動套用 Global Query Filter（IsDeleted = false），
/// 預設查詢（GetAll/Search）看不到已刪除的資料；需要看到已刪除資料時，Repository 方法會用
/// IgnoreQueryFilters() 明確繞過。GetByIdAsync 一律 IgnoreQueryFilters()（沿用 Phase 1 對 Product
/// 的既有行為：用 Id 查詢不受刪除狀態影響，例如作廢舊單據時仍要能找到已停用的商品）。
/// </summary>
public interface IAuditable : ITrackable
{
    bool IsDeleted { get; set; }
}
