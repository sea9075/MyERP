using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using MyErp.Application.Abstractions;

namespace MyErp.Infra.Messaging;

/// <summary>
/// 用 Azure Service Bus 實作 IEventPublisher（2026-09-15 新增，見 Infra-Progress.md §31）。
/// 佇列名稱讀 ServiceBus:QueueName（非機密設定，appsettings.json 有預設值 "sales-events"），
/// 連線字串讀 ServiceBus:ConnectionString（機密，走 dotnet user-secrets，見 DependencyInjection.cs）。
/// </summary>
public class ServiceBusEventPublisher(ServiceBusClient client, IConfiguration configuration) : IEventPublisher
{
    private readonly string queueName = configuration["ServiceBus:QueueName"] ?? "sales-events";

    public async Task PublishInventoryDecreasedAsync(InventoryDecreasedEvent @event, CancellationToken ct = default)
    {
        // ServiceBusClient 是單例、內部會自己管理連線，Sender 才是輕量、可以每次現用現建的物件
        // ——這是 Azure.Messaging.ServiceBus SDK 官方建議的用法。
        await using var sender = client.CreateSender(queueName);

        var message = new ServiceBusMessage(JsonSerializer.Serialize(@event))
        {
            ContentType = "application/json",
            Subject = nameof(InventoryDecreasedEvent),
        };

        await sender.SendMessageAsync(message, ct);
    }
}
