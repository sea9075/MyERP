using Microsoft.Extensions.Logging;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

/// <summary>
/// worker 端處理「庫存減少」事件的實際業務邏輯（2026-09-15 新增，見 Infra-Progress.md §31）。
/// 目前唯一的業務內容——低庫存自動通知：重新查一次商品「當下」的即時庫存（不是事件裡帶的舊值，
/// 見 InventoryDecreasedEvent 的說明），如果低於安全庫存、且目前沒有這個商品「未讀」的低庫存通知，
/// 就寫入一筆新通知。
///
/// 放在 Application 層（不是直接寫在 MyErp.Worker 專案裡），是因為這是「業務邏輯」，
/// 跟 MyErp.Api 呼叫 Service 的架構是同一套——MyErp.Worker 只負責訂閱 Service Bus、
/// 每則訊息開一個 DI scope 呼叫這裡，本身不包含任何商品/通知相關的判斷邏輯。
/// </summary>
public interface IInventoryEventHandler
{
    Task HandleInventoryDecreasedAsync(InventoryDecreasedEvent @event, CancellationToken ct = default);
}

public class InventoryEventHandler(
    IProductRepository productRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<InventoryEventHandler> logger) : IInventoryEventHandler
{
    public async Task HandleInventoryDecreasedAsync(InventoryDecreasedEvent @event, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdAsync(@event.ProductId, ct);
        if (product is null)
        {
            // 商品可能後來被刪除；不是需要重試的暫時性錯誤，記一筆警告後直接放過這則訊息。
            logger.LogWarning("低庫存通知：找不到商品 (Id={ProductId})，略過。", @event.ProductId);
            return;
        }

        if (product.CurrentStock >= product.SafetyStock)
        {
            return;
        }

        if (await notificationRepository.HasUnreadLowStockAsync(product.Id, ct))
        {
            // 已經有一筆未讀的低庫存通知在等使用者處理，不重複寫入，避免同一個商品洗版。
            return;
        }

        notificationRepository.Add(new Notification
        {
            Type = "LowStock",
            ProductId = product.Id,
            Message = $"商品「{product.Name}」庫存（{product.CurrentStock}）已低於安全庫存（{product.SafetyStock}），請盡快補貨。",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        });

        await unitOfWork.SaveChangesAsync(ct);
    }
}
