using MyErp.Application.Abstractions;

namespace MyErp.Infra.Messaging;

/// <summary>
/// 本機開發如果還沒設定 ServiceBus:ConnectionString 時的退回實作（2026-09-15 新增，
/// 見 DependencyInjection.cs 的註冊邏輯）。事件發布本來就是 best-effort（見 SalesOrderService／
/// PurchaseOrderService 呼叫端的 try/catch 說明），不應該因為沒設定訊息佇列就讓整個 API／
/// 單元測試開不起來，所以什麼都不做、直接成功回傳。
/// </summary>
public class NullEventPublisher : IEventPublisher
{
    public Task PublishInventoryDecreasedAsync(InventoryDecreasedEvent @event, CancellationToken ct = default) =>
        Task.CompletedTask;
}
