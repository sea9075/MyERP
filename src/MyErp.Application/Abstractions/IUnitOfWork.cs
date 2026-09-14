namespace MyErp.Application.Abstractions;

/// <summary>
/// 把「儲存變更」跟「交易範圍」抽象出來，Application 層只依賴這個介面，
/// 不需要知道底層是 EF Core 還是別的東西。
/// ERP.md §9 非功能性考量提到「庫存扣減仍建議在單一交易（Transaction）內完成，避免超賣」，
/// PurchaseOrderService / SalesOrderService 都是透過 ExecuteInTransactionAsync 來滿足這個要求。
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 在單一資料庫交易內執行 <paramref name="operation"/>；operation 內丟出的例外
    /// 會讓交易自動回滾（Rollback），不會有一半資料寫進去、一半沒寫的情況。
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
