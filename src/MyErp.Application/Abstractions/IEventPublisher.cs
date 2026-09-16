namespace MyErp.Application.Abstractions;

/// <summary>
/// 庫存減少事件（2026-09-15 新增：worker 低庫存自動通知，見 Infra-Progress.md §31）。
/// 出貨單建立、進貨單作廢都會讓庫存減少，兩者都會呼叫 IEventPublisher 發布這個事件，
/// 讓 MyErp.Worker（訂閱 Azure Service Bus 佇列 "sales-events"）非同步檢查是否要寫入低庫存通知，
/// 不會拖慢 API 本身建立/作廢單據的回應時間。
///
/// 事件內容刻意只帶 ProductId：worker 收到訊息時會重新查一次商品「當下」的即時庫存，
/// 而不是相信事件裡的舊快照——避免訊息在佇列裡等待處理的這段時間，庫存又被其他異動改變，
/// 造成 worker 依據過期資料做出錯誤判斷。
/// </summary>
public record InventoryDecreasedEvent(int ProductId);

/// <summary>
/// 把「發布事件到訊息佇列」抽象出來，Application 層只依賴這個介面，不需要知道底層是
/// Azure Service Bus 還是別的訊息系統。實作在 MyErp.Infra/Messaging/ 底下：
/// 正式環境用 ServiceBusEventPublisher，本機沒設定 Service Bus 連線字串時用 NullEventPublisher。
/// </summary>
public interface IEventPublisher
{
    Task PublishInventoryDecreasedAsync(InventoryDecreasedEvent @event, CancellationToken ct = default);
}
